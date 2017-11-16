--[[
    Setup the env
]]--

readonlytable = function(index)
    local protecteds = {}
    for k,v in pairs(index) do
        protecteds[k] = true
    end
    
    local meta = {
        __index = tbl,
        __newindex = function(tbl,key,value)
            if protecteds[key] then
                error("I'm sorry, Dave. I'm afraid I can't do that",0)
            else
                rawset(index,key,value)
            end
        end,
        __metatable = false,
    }

    return setmetatable({},meta);
end

ENV = {}

ENV.assert   = assert
ENV.error    = error
ENV.ipairs   = ipairs
ENV.next     = next
ENV.pairs    = pairs
ENV.select   = select
ENV.tonumber = tonumber
ENV.tostring = tostring 
ENV.unpack   = unpack
ENV._VERSION = _VERSION
ENV.xpcall   = xpcall
ENV.print    = print
ENV.type     = type

ENV.coroutine         = {}
ENV.coroutine.create  = coroutine.create
ENV.coroutine.resume  = coroutine.resume
ENV.coroutine.running = coroutine.running
ENV.coroutine.status  = coroutine.status
ENV.coroutine.wrap    = coroutine.wrap
ENV.coroutine.yield   = coroutine.yield
ENV.coroutine         = readonlytable(ENV.coroutine)

ENV.string         = {}
ENV.string.byte    = string.byte
ENV.string.char    = string.char
ENV.string.find    = string.find 
ENV.string.format  = string.format
ENV.string.gmatch  = string.gmatch
ENV.string.gsub    = string.gsub
ENV.string.len     = string.len
ENV.string.lower   = string.lower
ENV.string.match   = string.match
ENV.string.rep     = string.rep 
ENV.string.reverse = string.reverse 
ENV.string.sub     = string.sub
ENV.string.upper   = string.upper
ENV.string         = readonlytable(ENV.string)

ENV.table        = {}
ENV.table.insert = table.insert 
ENV.table.maxn   = table.maxn
ENV.table.remove = table.remove 
ENV.table.sort   = table.sort 
ENV.table        = readonlytable(ENV.table)

ENV.math            = {}
ENV.math.abs        = math.abs
ENV.math.acos       = math.acos 
ENV.math.asin       = math.asin
ENV.math.atan       = math.atan 
ENV.math.atan2      = math.atan2 
ENV.math.ceil       = math.ceil 
ENV.math.cos        = math.cos 
ENV.math.cosh       = math.cosh 
ENV.math.deg        = math.deg
ENV.math.exp        = math.exp
ENV.math.floor      = math.floor
ENV.math.fmod       = math.fmod
ENV.math.frexp      = math.frexp
ENV.math.huge       = math.huge 
ENV.math.ldexp      = math.ldexp
ENV.math.log        = math.log 
ENV.math.log10      = math.log10
ENV.math.max        = math.max
ENV.math.min        = math.min 
ENV.math.modf       = math.modf
ENV.math.pi         = math.pi 
ENV.math.pow        = math.pow
ENV.math.rad        = math.rad 
ENV.math.random     = math.random 
ENV.math.randomseed = math.randomseed
ENV.math.sin        = math.sin
ENV.math.sinh       = math.sinh 
ENV.math.sqrt       = math.sqrt 
ENV.math.tan        = math.tan 
ENV.math.tanh       = math.tanh
ENV.math            = readonlytable(ENV.math)

ENV.os       = {}
ENV.os.clock = os.clock 
ENV.os.time  = os.time
ENV.os       = readonlytable(ENV.os)

--[[
    Extra funcs for utils
]]--
isnumber = function(what)
    return type(what) == "number"
end
ENV.isnumber = isnumber

istable = function(what)
    return type(what) == "table"
end
ENV.istable = istable

isstring = function(what)
    return type(what) == "string"
end
ENV.isstring = isstring

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

printtable = function( t, indent, done )
    done = done or {}
    indent = indent or 0
    
    local keys = getkeys(t)
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
ENV.printtable = printtable

--[[
    Sandbox untrusted scripts
]]--
local PRINT_PILE = {}
print = function(...)
    table.insert(PRINT_PILE,{...})
end

sandbox = function(script)
    if script:byte(1) == 27 then 
        return nil, "HAX!" 
    end
    
    local script, message = loadstring(script)
    if not script then 
        return nil,message 
    end
    
    setfenv(script,ENV)
    
    local t = os.time()
    debug.sethook(function()
        local diff = os.time() - t
        if diff > 400000 then
            t = os.time()
            error("My god what are you doing?!",0)
        end
    end,"l",2)
    
    local succ,err = pcall(script)

    debug.sethook()
    
    local printstack = ""
    for pr,_ in ipairs(PRINT_PILE) do
        printstack = printstack .. table.concat(pr,",",0,#pr) .. "\n"
    end
    table.empty(PRINT_PILE)
    
    return succ,err,printstack
end