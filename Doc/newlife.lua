do
    local p_newlife = Proto("newlife", "新生命标准网络封包")

    -- https://www.wireshark.org/docs/wsdg_html_chunked/lua_module_Proto.html#lua_class_ProtoField
    local FF_flag = {
        [8] = "[Reply]",
        [7] = "[Error/Oneway]",
        [3] = "[Encrypted]",
        [2] = "[Compressed]",
        [1] = "[Binary]"
    }

    local f_flag = ProtoField.uint8("NewLife.flag", "标记", base.HEX, FF_flag, 0xFF)
    -- local f_flag = ProtoField.uint8("NewLife.flag", "标记", base.HEX)
    local f_seq = ProtoField.uint8("NewLife.seq", "序列号", base.DEC)
    local f_length = ProtoField.uint16("NewLife.length", "长度", base.DEC)
    -- local f_data = ProtoField.string("NewLife.data", "内容", base.UNICODE)
    local f_data = ProtoField.bytes("NewLife.data", "数据", base.SPACE)

    p_newlife.fields = {f_flag, f_seq, f_length, f_data}

    local data_dis = Dissector.get("data")

    local function NewLife_dissector(buf, pkt, root)
        local buf_len = buf:len();
        if buf_len < 4 then
            return false
        end

        local tvb = buf:range()
        local v_flag = tvb(0, 1)
        local v_seq = tvb(1, 1)
        local v_length = tvb(2, 2)
        local flag = tvb(0, 1):uint()

        local len = tvb(2, 2):le_uint()
        local v_data = tvb(4, len)

        pkt.cols.protocol = "NewLife"

        local t = root:add(p_newlife, buf)
        t:add(f_flag, v_flag)
        t:add(f_seq, v_seq)
        t:add_le(f_length, v_length)

        -- t:add_packet_field(f_data, v_data, ENC_UTF_8 + ENC_STRING)
        t:add(f_data, v_data)

        return true
    end

    function p_newlife.dissector(buf, pkt, root)
        if NewLife_dissector(buf, pkt, root) then
            -- valid NewLife diagram
        else
            data_dis:call(buf, pkt, root)
        end
    end

    local udp_encap_table = DissectorTable.get("udp.port")
    udp_encap_table:add(5500, p_newlife)
    udp_encap_table:add(9999, p_newlife)
    udp_encap_table:add(777, p_newlife)
    udp_encap_table:add(12345, p_newlife)

    local tcp_encap_table = DissectorTable.get("tcp.port")
    tcp_encap_table:add(5500, p_newlife)
    tcp_encap_table:add(9999, p_newlife)
    tcp_encap_table:add(777, p_newlife)
    tcp_encap_table:add(12345, p_newlife)
end
