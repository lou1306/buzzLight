#!/usr/bin/env python3
import begin
import re
import platform
import sys
from subprocess import check_output, DEVNULL, CalledProcessError
from enum import Enum
from os import remove
import uuid
from cex import translateCPROVER

SYS = platform.system()


class Backends(Enum):
    CBMC = "cbmc"
    ESBMC = "esbmc"


backend_descr = """choose the verification backend.
Options are: {}""".format(", ".join(b.value for b in Backends))

timeout_descr = """configure time limit (seconds).
A value of 0 means no timeout (default)"""

values_descr = "specify values for parameterised specification (key=value)"

backends = {
    "cbmc": ["cbmc"],
    "esbmc": [
        "esbmc", "--no-bounds-check", "--no-div-by-zero-check",
        "--no-pointer-check", "--no-align-check", "--no-unwinding-assertions",
        "--z3"]
}

backends_debug = {
    "cbmc": ["cbmc", "--bounds-check" "--signed-overflow-check"],
    "esbmc": [
        "esbmc", "--no-pointer-check", "--no-align-check",
        "--no-unwinding-assertions", "--z3"]
}


def check_cbmc_version():
    cbmc_check = ["cbmc", "--version"]
    CBMC_V, CBMC_SUBV = check_output(cbmc_check).decode().strip().split(".")
    if not (int(CBMC_V) <= 5 and int(CBMC_SUBV) <= 4):
        backends["cbmc"].append("--trace")
        backends_debug["cbmc"].append("--trace")


def unwind(backend, num):
    return {
        "cbmc":
            ["--unwindset",
                "confirm.0:{0},propagate.0:{0},differentLstig.0:{0}".format(
                    num)],
        "esbmc":
            ["--unwindset", "1:{0},2:{0},4:{0}".format(num)]

    }[backend]


cmd = "core/LabsTranslate"

if "Linux" in SYS:
    env = {"LD_LIBRARY_PATH": "core/libunwind"}
    TIMEOUT_CMD = "/usr/bin/timeout"
else:
    env = {}
    TIMEOUT_CMD = "/usr/local/bin/gtimeout"


class Components:
    def __init__(self, d):
        self._dict = d

    def __getitem__(self, key):
        for (a, b), v in self._dict.items():
            if a <= key <= b:
                return v
        raise KeyError


def split_comps(c):
    result = {}
    for comp in c.split(";"):
        name, rng = comp.split(" ")
        compmin, compmax = rng.split(",")
        result[(int(compmin), int(compmax))] = name
    return Components(result)


def gather_info(call):
    call_info = call + ["--info"]
    info = check_output(call_info, env=env)
    # Deserialize system info
    i_names, l_names, e_names, comps, unwind, *_ = info.decode().split("\n")
    info = {
        "I": i_names.split(","),
        "L": l_names.split(","),
        "E": e_names.split(","),
        "Comp": split_comps(comps),
        "unwind": unwind.split(" ")[1]
    }
    return info


def parse_linux(file, values, bound, fair, simulate):
    call = [
        cmd,
        "--file", file,
        "--bound", str(bound)]
    if values:
        call.extend(["--values"] + list(values))
    if fair:
        call.append("--fair")
    if simulate:
        call.append("--simulation")
    try:
        out = check_output(call, env=env)
        fname = str(uuid.uuid4()) + ".c"

        with open(fname, 'wb') as out_file:
            out_file.write(out)
        return out.decode("utf-8"), fname, gather_info(call)
    except CalledProcessError as e:
        print(e, file=sys.stderr)
        return None, None, None


def get_functions(c_program):
    isFunc = re.compile(r"^void (\w+)\(int tid\)")

    lines = c_program.split("\n")
    lines = (
        (isFunc.match(l1).group(1), l2.split("// ")[1])
        for l1, l2 in zip(lines, lines[1:])
        if isFunc.match(l1))
    return dict(lines)


@begin.start(auto_convert=True)
def main(file: "path to LABS file",
         backend: backend_descr = "cbmc",
         steps: "number of system evolutions" = 1,
         fair: "enforce fair interleaving of components" = True,
         simulate: "run in simulation mode" = False,
         show: "print C encoding and exit" = False,
         debug: "enable additional checks in the backend" = False,
         timeout: timeout_descr = 0,
         *values: values_descr):
    """ SLiVER - Symbolyc LAbS VERification.
"""
    print("Encoding...", file=sys.stderr)
    c_program, fname, info = parse_linux(file, values, steps, fair, simulate)
    if fname:
        if show:
            print(c_program)
            return
        if backend == "cbmc":
            check_cbmc_version()
        backend_call = backends_debug[backend] if debug else backends[backend]
        backend_call.append(fname)
        backend_call.extend(unwind(backend, info["unwind"]))
        if timeout > 0:
            backend_call = [TIMEOUT_CMD, str(timeout)] + backend_call
        try:
            sim_or_verify = "Running simulation" if simulate else "Verifying"
            print(
                "{} with backend {}...".format(sim_or_verify, backend),
                file=sys.stderr)
            out = b''
            out = check_output(backend_call, stderr=DEVNULL)
        except KeyboardInterrupt as err:
            print("Verification stopped (keyboard interrupt)", file=sys.stderr)
        except CalledProcessError as err:
            if err.returncode == 10:
                out = err.output
            elif err.returncode == 6:
                print("Backend failed with parsing error.", file=sys.stderr)
            elif err.returncode == 124:
                print(
                    "Timed out after {} seconds"
                    .format(timeout), file=sys.stderr)
            else:
                print(
                    "Unexpected error (return code: {})"
                    .format(err.returncode), file=sys.stderr)
        finally:
            out = out.decode("utf-8")
            remove(fname)
            if ("VERIFICATION SUCCESSFUL" in out):
                print("No properties violated!", end="", file=sys.stderr)
                if simulate:
                    print(" (simulation mode)", file=sys.stderr)
            else:
                print(translateCPROVER(out, c_program, info))
                # cex_cbmc(out, i_names, l_names)