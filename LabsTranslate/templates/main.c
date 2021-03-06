void monitor() {
    {%- for item in alwaysasserts -%}
    __CPROVER_assert({{item.value}}, "{{item.name}}");
    {%- endfor -%}
}

{%- if bound > 0 -%}
void finally() {
    {%- for item in finallyasserts -%}
    __CPROVER_assert({{item.value}}, "{{item.name}}");
    {%- endfor -%}
    {%- if simulation -%}
    __CPROVER_assert(0);
    {%- endif -%}
}
{%- endif -%}

int main(void) {
    init();
    monitor(); // Check invariants on the initial state
    TYPEOFAGENTID firstAgent{% if firstagent == 0 and fair %} = 0;{% else %};
    __CPROVER_assume(firstAgent < MAXCOMPONENTS);
    {% endif %};

    {%- if hasStigmergy and bound > 0 -%}
    _Bool sys_or_not[BOUND];
    {%- endif -%}

    {%- if bound > 0 -%}
    unsigned __LABS_step;
    for (__LABS_step=0; __LABS_step<BOUND; __LABS_step++) {
    {%- else -%}
    while(1) {        
    {%- endif -%}
        // if (terminalState()) break;
        
        {%- if hasStigmergy -%}{%- if bound > 0 -%}
        if (sys_or_not[__LABS_step]) {
        {%- else -%}
        if ((_Bool) __CPROVER_nondet()) {
        {%- endif -%}{%- endif -%}
            {%- unless fair -%}
            TYPEOFAGENTID nextAgent;
            __CPROVER_assume(nextAgent < MAXCOMPONENTS);
            firstAgent = nextAgent;
            {%- endunless -%}

            switch (pc[firstAgent][0]) {
            {%- for item in schedule -%}
                case {{ item.entry.first.value }}: {{ item.name }}(firstAgent); break;
            {%- endfor -%}
              default: 
                {%- if bound > 0 -%}
                __CPROVER_assume(0);
                {%- else -%}
                {}
                {%- endif -%}
            }
            
            {%- if fair -%}
            if (firstAgent == MAXCOMPONENTS - 1) {
                firstAgent = 0;
            }
            else {
                firstAgent++;
            }
            {%- endif -%}
        {%- if hasStigmergy -%}
        }
        else {
            _Bool propagate_or_confirm; 
            if (propagate_or_confirm) propagate();
            else confirm();
        }
        {%- endif -%}
        monitor();

        {%- if finallyasserts and finallyasserts.size > 0 and bound == 0 -%}
        if ({%- for item in finallyasserts -%}{{item.value}}{%- endfor -%}) { 
            return 0; 
        }
        {%- endif -%}
    }
    {%- if finallyasserts and finallyasserts.size > 0 and bound > 0 -%}
    finally();
    {%- endif -%}
}
