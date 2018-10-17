void {{label}}(int tid) {
    //{{labs}}

{%- include "templates/entry" -%}

    {%- for item in assignments -%}
    int val{{forloop.index0}} = {{item.expr}};
    {%- if item.size != 0 -%}
    int offset{{forloop.index0}} = {{item.offset}};
    assert(offset{{forloop.index0}} >= 0 && offset{{forloop.index0}} < {{item.size}});
    {%- endif -%}{%- endfor -%}

    {%- for item in assignments -%}
    {%- capture check -%}{%- if forloop.first -%}1{%- else -%}0{%- endif -%}{%- endcapture -%}
    {%- if item.size != 0 -%}
    {{type}}(tid, {{item.key}} + offset{{forloop.index0}}, val{{forloop.index0}}, {{check}});
    {%- else -%}
    {{type}}(tid, {{item.key}}, val{{forloop.index0}}, {{check}});
    {%- endif -%}{%- endfor -%}
    {%- for k in qrykeys -%}
    setHin(tid, {{k}});{%- endfor -%}

    {%- for item in exitpoints -%}
    pc[tid][{{ item.pc }}] = {{ item.value }};
    {%- endfor -%}
}
