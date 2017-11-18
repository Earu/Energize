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

gettableaddress = function(tbl)
    if not type(tbl) == "table" then return "0x00000000" end
    local address = (tostring(tbl):gsub("table: ",""))
    return address
end

local getmeta = getmetatable
local setmeta = setmetatable
local protecteds = {}

local protect = function(tbl)
    protecteds[gettableaddress(tbl)] = true
end

local isprotected = function(tbl)
    return protecteds[gettableaddress(tbl)]
end

setup = function()
    local placeholder = function(...)
        print('nope.')
    end

    local ENV = {}
    
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
    ENV.pcall    = pcall
    ENV.print    = print
    ENV.type     = type
    ENV._G       = ENV
    protect(ENV)

    ENV.coroutine = coroutine
    ENV.table     = table
    ENV.math      = math
    ENV.string    = string
    ENV.os        = os
    ENV.event     = event
    protect(ENV.coroutine)
    protect(ENV.table)
    protect(ENV.math)
    protect(ENV.string)
    protect(ENV.os)
    protect(ENV.event)

    ENV.string.dump  = placeholder
    ENV.os.execute   = placeholder
    ENV.os.date      = placeholder
    ENV.os.difftime  = placeholder
    ENV.os.exit      = placeholder
    ENV.os.getenv    = placeholder
    ENV.os.remove    = placeholder
    ENV.os.rename    = placeholder
    ENV.os.setlocale = placeholder
    ENV.os.tmpname   = placeholder

    ENV.getmetatable = getmeta
    ENV.setmetatable = function(t1,t2)
        if isprotected(t1) then
            error("I'm sorry, Dave. I'm afraid I can't do that",0)
        else
            return setmeta(t1,t2)
        end
    end
    
    return readonlytable(ENV)
end

--[[
    Sandbox untrusted scripts
]]--
ENV = setup()

safefunc = function(resulttbl,func,...)
    local resulttbl = resulttbl or {
        Success = true,
        Error = "",
        Varargs = {},
        PrintStack = "",
    }

    setfenv(func,ENV)
    
    local t = os.time()
    debug.sethook(function()
        local diff = os.time() - t
        if diff > 1000000 then
            t = os.time()
            error("My god what are you doing?!",0)
        end
    end,"l",2)
    local retrieved = { pcall(func,...) }
    debug.sethook()

    local succ,err = retrieved[1],retrieved[2]
    local varargs = {}
    local printstack = ""
    
    for i=2,#retrieved do
        table.insert(varargs,(retrieved[i] == nil and "nil" or retrieved[i]))
    end

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
    
    resulttbl.Success = succ
    resulttbl.Error = err
    resulttbl.Varargs = varargs
    resulttbl.PrintStack = printstack

    return resulttbl
end

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
    
    local script,message = loadstring(script)

    if not script then 
        result.Success = false
        result.Error = message
        
        return result
    end
    
    return safefunc(result,script)
end