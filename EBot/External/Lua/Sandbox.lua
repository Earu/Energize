--[[
    Setup the env
]]--
readonlytable = function(index)
    local protecteds = {}
    for k,v in pairs(index) do
        protecteds[k] = true
    end
    
    local meta = {
        __index = index,
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

local PRINT_PILE = {}
print = function(...)
    table.insert(PRINT_PILE,{...})
end

local placeholder = function(...)
    print('nope.')
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

ENV.coroutine = coroutine
ENV.coroutine = readonlytable(ENV.coroutine)

ENV.string      = string
ENV.string.dump = placeholder
ENV.string      = readonlytable(ENV.string)

ENV.table = table
ENV.table = readonlytable(ENV.table)

ENV.math = math
ENV.math = readonlytable(ENV.math)

ENV.os           = os
ENV.os.execute   = placeholder
ENV.os.date      = placeholder
ENV.os.difftime  = placeholder
ENV.os.exit      = placeholder
ENV.os.getenv    = placeholder
ENV.os.remove    = placeholder
ENV.os.rename    = placeholder
ENV.os.setlocale = placeholder
ENV.os.tmpname   = placeholder
ENV.os           = readonlytable(ENV.os)


--[[
    Sandbox untrusted scripts
]]--
local env = readonlytable(ENV)
sandbox = function(script)
    local result = {
        Success = true,
        Error = "",
        Varargs = {},
        PrintStack = "",
    }

    if script:byte(1) == 27 then 
        result.Success = false
        result.Error = "HAX!"

        return result
    end
    
    local script, message = loadstring(script)
    if not script then 
        result.Success = false
        result.Error = message
        
        return result
    end
    
    setfenv(script,env)
    
    local t = os.time()
    debug.sethook(function()
        local diff = os.time() - t
        if diff > 400000 then
            t = os.time()
            error("My god what are you doing?!",0)
        end
    end,"l",2)
    local retrieved = { pcall(script) }
    debug.sethook()
    
    local succ,err = retrieved[1],retrieved[2]
    
    local varargs = {}
    for i=2,#retrieved do
        table.insert(varargs,(retrieved[i] == nil and "nil" or retrieved[i]))
    end
    local printstack = ""
    
    for _,pr in ipairs(PRINT_PILE) do
        local lastkey = 1
        local currentkey = 1
        for k,val in pairs(pr) do
            currentkey = k
            for i=1,currentkey - lastkey - 1 do
                printstack = printstack .. "nil\t"
            end
            lastkey = k
            printstack = printstack .. tostring(val) .. "\t"
        end
        printstack = string.sub(printstack,1,#printstack - 1)
        printstack = printstack .. "\n"
    end
    table.empty(PRINT_PILE)

    result.Success = succ
    result.Error = err
    result.Varargs = varargs
    result.PrintStack = printstack
    
    return result
end