void init() {
    unsigned char j = 0;

    {%- for agent in agents -%}
    {%- assign a = agent.end | minus: 1 -%}
    {%- for i in (agent.start..a) -%}

    for (j=0; j<MAXKEYI; j++) {
        I[{{i}}][j] = __CPROVER_nondet_int();
    }
    {%- if hasStigmergy -%}
    for (j=0; j<MAXKEYL; j++) {
        Lvalue[{{i}}][j] = __CPROVER_nondet_int();
        Ltstamp[{{i}}][j] = 0;
        Hin[{{i}}][j] = 0;
        Hout[{{i}}][j] = 0;
    }
    HinCnt[{{i}}] = 0;
    HoutCnt[{{i}}] = 0;
    {%- endif -%}

    {%- for p in agent.pcs -%}
    {%- if p.value.size == 1 -%}
    pc[{{i}}][{{ p.name }}] = {{ p.value.first }};
    {%- else -%}
    pc[{{i}}][{{ p.name }}] = __CPROVER_nondet_int();
    __CPROVER_assume({%- for val in p.value -%} (pc[{{i}}][{{ p.name }}] == {{ val }}){% unless forloop.last %} | {% endunless %}{%- endfor-%});
    {%- endif -%}{%- endfor -%}{%- endfor -%}{%- endfor -%}
        
    {%- for item in initenv -%}
        {%- if item.bexpr contains "&" or item.bexpr contains "|" or item.bexpr contains "<" or item.bexpr contains "!" or item.bexpr contains ">" -%}
    E[{{item.index}}] = __CPROVER_nondet_int();
    __CPROVER_assume({{ item.bexpr }});
        {%- else -%}
    {{ item.bexpr | replace: "==", "=" }};
        {%- endif -%}
    {%- endfor -%}
    {%- for agent in agents -%}
    {%- for item in agent.initvars -%}
        {%- if item.bexpr contains "&" or item.bexpr contains "|" or item.bexpr contains "<" or item.bexpr contains "!" or item.bexpr contains ">" -%}
    __CPROVER_assume({{ item.bexpr }});
        {%- else -%}
    {{ item.bexpr | replace: "==", "=" }};
        {%- endif -%}
    {%- endfor -%}
    {%- endfor -%}
    {%- if hasStigmergy -%}
    {%- for item in tstamps -%}
    Ltstamp[{{item.tid}}][tupleStart[{{item.index}}]] = now();
    {%- endfor -%}
    now();
    {%- endif -%}

}
