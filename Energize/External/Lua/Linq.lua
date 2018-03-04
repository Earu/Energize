local Linq = {}
local table = _G.table
_G.Linq = Linq
_G.Lq = Linq

Linq.Array = function(tbl)
    return setmetatable(tbl, {
        __index = Linq,
    })
end

Linq.Select = function(tbl,predicate)
    local t = {}
    local len = Linq.Count(tbl)
    for i=1,len do
        local k,v = i,tbl[i]
        local res = predicate(v) or v
        table.insert(t,res)
    end

    return Linq.Array(t)
end

Linq.Where = function(tbl,predicate)
    local t = {}
    local len = Linq.Count(tbl)
    for i=1,len do
        local k,v = i,tbl[i]
        local res = predicate(v) or false
        if res then
            table.insert(t,v)
        end
    end

    return Linq.Array(t)
end

Linq.Any = function(tbl,predicate)
    predicate = predicate or function() return true end
    local len = Linq.Count(tbl)
    for i=1,len do
        local k,v = i,tbl[i]
        if predicate(v) then
            return true
        end
    end

    return false
end

Linq.Count = function(tbl)
    local count = 0
    for k,v in pairs(tbl) do
        count = count + 1
    end

    return count
end

Linq.Sum = function(tbl)
    local sum = 0
    for k,v in pairs(tbl) do
        if type(v) == "number" then
            sum = sum + v
        end
    end

    return sum
end

Linq.Type = function(tbl)
    local type
    for k,v in pairs(tbl) do
        local t = _G.type(v)
        if type and t ~= type then
            type = "dynamic"
            break
        else
            type = t
        end
    end

    return type
end

Linq.TypeOf = function(tbl,index)
    return type(tbl[index])
end

Linq.First = function(tbl,predicate)
    predicate = predicate or function() return true end
    return Linq.Where(tbl,predicate)[1]
end

Linq.Last = function(tbl,predicate)
    predicate = predicate or function() return true end
    local t = Linq.Where(tbl,predicate)
    return t[#t]
end

Linq.Distinct = function(tbl)
    local t = {}
    local used = {}
    for k,v in pairs(tbl) do
        local k,v = i,tbl[i]
        if not used[v] then
            used[v] = true
            table.insert(t,v)
        end
    end

    return Linq.Array(t)
end

Linq.Reverse = function(tbl)
    local len = #tbl
    if len <= 0 then return tbl end
    local t = {}
    local temp
    for i=1,len do
        temp = tbl[i]
        t[i] = tbl[len - i]
        t[len - i] = temp
    end

    return Linq.Array(t)
end

Linq.Join = function(tbl,sep)
    return table.concat(tbl,sep)
end

Linq.Sort = function(tbl,predicate)
    local t = tbl
    table.sort(t,predicate)

    return Linq.Array(t)
end

Linq.ForEach = function(tbl,predicate)
    local t = tbl
    for k,v in pairs(tbl) do
        local k,v = i,tbl[i]
        predicate(v)
    end
end

Linq.IndexOf = function(tbl,val)
    for k,v in pairs(tbl) do
        if type(v) == "table" then
            if Lq.Equals(v,val) then
                return k
            end
        else
            if v == val then
                return k
            end
        end
    end

    return -1
end

Linq.ToString = function(tbl)
    local t = {}
    for k,v in pairs(tbl) do
        t[k] = tostring(v)
    end

    return Linq.Array(t)
end

local function DeepClone(tbl)
    local t = {}
    for k,v in pairs(tbl) do
        if type(v) == "table" then
            t[k] = DeepClone(v)
        else
            t[k] = v
        end
    end

    return Linq.Array(t)
end

Linq.Clone = function(tbl,deep)
    deep = deep or false
    if deep then
        return DeepClone(tbl)
    else
        local t = {}
        for k,v in pairs(tbl) do
            t[k] = v
        end

        return Linq.Array(t)
    end
end

Linq.Equals = function(source,tbl)
    for k,v in pairs(tbl) do
        if source[k] ~= v then
            return false
        end
    end

    for k,v in pairs(source) do
        if tbl[k] ~= v then
            return false
        end
    end

    return true
end

Linq.Append = function(tbl,val)
    local t = tbl
    table.insert(t,val)
    return Linq.Array(t)
end

Linq.AppendFirst = function(tbl,val)
    local t = tbl
    table.insert(t,1,val)
    return Linq.Array(t)
end

Linq.AppendAt = function(tbl,index,val)
    local t = tbl
    table.insert(t,index,val)
    return Linq.Array(t)
end

Linq.RemoveFirst = function(tbl)
    local t = tbl
    table.remove(tbl,1)
    return Linq.Array(t)
end

Linq.RemoveLast = function(tbl)
    local t = tbl
    table.remove(t,#tbl)
    return Linq.Array(t)
end

Linq.RemoveAt = function(tbl,index)
    local t = tbl
    table.remove(t,index)
    return Linq.Array(t)
end

Linq.Remove = function(tbl,val)
    local t = tbl
    local i = Linq.IndexOf(tbl,val)
    table.remove(t,i)
    return Linq.Array(t)
end

