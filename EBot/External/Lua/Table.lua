table.getkeys = function(tab)
    local keys = {}
    local id = 1
    for k, v in pairs(tab) do
        keys[id] = k
        id = id + 1
    end
    return keys
end
ENV.table.getkeys = table.getkeys

table.copy = function(t,lookup_table)
	if t == nil then return nil end
    
    local copy = {}
	setmetatable(copy,debug.getmetatable(t))
    
    for i,v in pairs(t) do
		if not istable(v)  then
			copy[i] = v
		else
			lookup_table = lookup_table or {}
			lookup_table[t] = copy
			if lookup_table[v] then
				copy[i] = lookup_table[v] -- we already copied this table. reuse the copy.
			else
				copy[i] = table.copy(v,lookup_table) -- not yet copied. copy it.
			end
		end
	end
    
    return copy
end
ENV.table.copy = table.copy

table.empty = function(t)
    for k,v in pairs(t) do
        t[k] = nil
    end
end
ENV.table.empty = table.empty