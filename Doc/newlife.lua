do
    local p_newlife = Proto("newlife", "新生命标准网络封包")

    -- https://www.wireshark.org/docs/wsdg_html_chunked/lua_module_Proto.html#lua_class_ProtoField
    local FF_flag = {
        [0x80] = "[Reply]",
        [0x81] = "[Reply]",
        [8] = "[Reply]",
        [7] = "[Error/Oneway]",
        [2] = "[Json]",
        [1] = "[Binary]"
    }

    local f_flag = ProtoField.uint8("NewLife.flag", "标记", base.HEX, FF_flag, 0xFF)
    local f_seq = ProtoField.uint8("NewLife.seq", "序列号", base.DEC)
    local f_length = ProtoField.uint16("NewLife.length", "长度", base.DEC, nil, "字节长度")
    local f_data = ProtoField.bytes("NewLife.data", "数据", base.SPACE)

    p_newlife.fields = {f_flag, f_seq, f_length, f_data}

    local data_dis = Dissector.get("data")

    local function NewLife_dissector(buffer, pinfo, tree)
        if buffer:len() < 4 then return false end

        local flags = buffer(0, 1):uint()
        local seq = buffer(1, 1):uint()
        local len = buffer(2, 2):le_uint()

        if 4 + len ~= buffer:len() then return false end

        pinfo.cols.protocol = "NewLife"

        local t = tree:add(p_newlife, buffer)
        t:add(f_flag, buffer(0, 1), flags)
        t:add(f_seq, buffer(1, 1), seq)
        local len_item = t:add_le(f_length, buffer(2, 2), len)
  
        -- 检查负载数据长度是否超出实际捕获的数据长度  
        if buffer:len() - 4 < len then  
            len_item:add_expert_info(PI_MALFORMED, PI_WARN, "Payload length is beyond the end of the packet")  
            return  
        end 

        if len > 0 then
            t:add(f_data, buffer(4, len), "Payload")
        end

        return true
    end

    function p_newlife.dissector(buffer, pinfo, tree)
        if NewLife_dissector(buffer, pinfo, tree) then
            -- valid NewLife diagram
        else
            data_dis:call(buffer, pinfo, tree)
        end
    end

    -- register_postdissector(p_newlife)

    -- DissectorTable.new("newlife")
    
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

    DissectorTable.new("newlife")
end
