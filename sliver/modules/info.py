"""Functions and classes to obtain and represent structured information
about a LAbS system
"""
import platform
from subprocess import check_output
from random import choice

SYS = platform.system()

if "Linux" in SYS:
    env = {"LD_LIBRARY_PATH": "labs/libunwind"}
    TIMEOUT_CMD = "/usr/bin/timeout"
else:
    env = {}
    TIMEOUT_CMD = "/usr/local/bin/gtimeout"


class Info(object):
    def __init__(self, spawn, e):
        self.spawn = spawn
        self.i = {}
        self.lstig = {}
        for c in spawn.values():
            self.i.update(c.iface)
            self.lstig.update(c.lstig)
        self.e = e

    @staticmethod
    def parse(txt):
        if not txt:
            return Info(Spawn({}), {})
        """Deserialize system info
        """
        lines = txt.split("|")
        envs, comps = lines[0], lines[1:]
        return Info(
            spawn=Spawn.parse(comps),
            e=[Variable(*v.split("=")) for v in envs.split(";") if v])

    def instrument(self):
        # max_index = info["Comp"].num_agents() - 1
        def format(fmt, var, index):
            return ",".join(
                fmt.format(TYPE, index + i, var.rnd_value())
                for i in range(var.size))

        def fmt(location, var, offset=0):
            return [
                (TYPE, location, offset + var.index + i, var.rnd_value())
                for i in range(var.size)
            ]

        TYPE = "short"
        out = [fmt("E", x) for x in self.e]
        for (low, up), agent in self.spawn.items():
            i_length = len(agent.iface)
            out.extend(
                fmt("I", x, n * i_length)
                for n in range(low, up)
                for x in agent.iface.values())
            out.extend(
                fmt("Lvalue", x, n * i_length)
                for n in range(low, up)
                for x in agent.lstig.values())
        # Return the flattened list
        return (x for l in out for x in l)


class Spawn:
    """Maps ids to agents in the system.
    """

    def __init__(self, d):
        self._dict = d

    def __getitem__(self, key):
        """spawn[tid] returns the agent definition for agent tid
        """
        for (a, b), v in self.items():
            if a <= key < b:
                return v
        raise KeyError

    def num_agents(self):
        """Returns the total number of agents in the system
        """
        return max(self._dict.keys(), key=lambda x: x[1])[1]

    def values(self):
        """Exposes the values of the internal dictionary
        """
        return self._dict.values()

    def items(self):
        """Exposes the items of the internal dictionary
        """
        return self._dict.items()

    @staticmethod
    def parse(c):
        result = {}

        for comp, iface, lstig in zip(c[::3], c[1::3], c[2::3]):
            name, rng = comp.split(" ")
            compmin, compmax = rng.split(",")
            result[(int(compmin), int(compmax))] = Agent(name, iface, lstig)

        return Spawn(result)


class Variable:
    """Representation of a single variable
    """

    def __init__(self, index, name, init):
        """Summary

        Args:
            index (TYPE): Description
            name (TYPE): Description
            init (TYPE): Description
        """
        self.index = int(index)
        self.size = 1
        if "[" in name:
            self.name, size = name.split("[")
            self.size = int(size[:-1])
        else:
            self.name = name
        if init[0] == "[":
            self.values = [int(v) for v in init[1:-1].split(",")]
        elif ".." in init:
            low, up = init.split("..")
            self.values = range(int(low), int(up))
        elif init == "undef":
            self.values = [-32767]  # UNDEF

    def rnd_value(self):
        """Returns a random, feasible initial value for the variable.
        """
        return choice(self.values)


class Agent:

    def __init__(self, name, iface, lstig):
        self.name = name
        self.iface = {}
        self.lstig = {}

        if iface != "":
            for txt in iface.split(";"):
                splitted = txt.split("=")
                index, text = splitted[0], splitted[1:]
                self.iface[int(index)] = Variable(int(index), *text)

        if lstig != "":
            for txt in lstig.split(";"):
                splitted = txt.split("=")
                index, text = splitted[0], splitted[1:]
                self.lstig[int(index)] = Variable(int(index), *text)

    def __str__(self):
        return self.name


def raw_info(call):
    call_info = call + ["--info"]
    return check_output(call_info, env=env)