isnumber = function(what)
    return type(what) == "number"
end

istable = function(what)
    return type(what) == "table"
end

isstring = function(what)
    return type(what) == "string"
end

isfunction = function(what)
    return type(what) == "function"
end

printtable = function( t, indent, done )
    done = done or {}
    indent = indent or 0
    
    local keys = table.getkeys(t)
    table.sort(keys, function(a,b)
        if isnumber(a) and isnumber(b) then return a < b end
        return tostring(a) < tostring(b)
    end)
    
    for i = 1, #keys do
        local key = keys[i]
        local value = t[key]
        print(string.rep("\t",indent))
        if istable(value) and not done[value] then
            done[value] = true
            print(tostring(key) .. ":" .. "\n")
            printtable(value,indent + 2,done)
            done[value] = nil
        else
            print(tostring(key) .. "\t=\t")
            print(tostring(value) .. "\n")
        end
    end

end