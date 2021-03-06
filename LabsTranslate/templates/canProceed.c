_Bool canProceed(int tid) {
    // Returns 1 if agent tid can perform at least one transition
    if (pc[tid][0] == 0) return 0;
    {%- for g in guards -%}
    if ({{ g | join: " && " }}) return 1;
    {%- endfor -%}
    return 0;
}

_Bool terminalState() {
    // Returns 1 if the system is in a terminal state.
    for (unsigned i=0; i<MAXCOMPONENTS; i++) {
        if (canProceed(i)) return 0;
        if (HinCnt[i] > 0 || HoutCnt[i] > 0) return 0;
    }
    return 1;
}
