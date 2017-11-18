local eventhandlers = {}

event = {
    attach = function(e,id,handler)
        if not e or (e and type(e) ~= "string") then
            error("bad argument #1 to 'attach' (string expected)")
        end
        if not id or (id and type(id) ~= "string") then
            error("bad argument #2 to 'attach' (string expected)")
        end
        if not handler or (handler and type(handler) ~= "function") then
            error("bad argument #3 to 'attach' (function expected)")
        end
        eventhandlers[e] = eventhandlers[e] or {}
        eventhandlers[e][id] = handler
    end,
    detach = function(e,id)
        if not e or (e and type(e) ~= "string") then
            error("bad argument #1 to 'detach' (string expected)")
        end
        if not id or (id and type(id) ~= "string") then
            error("bad argument #2 to 'detach' (string expected)")
        end
        if eventhandlers[e] then
            eventhandlers[e][id] = nil
        end
    end,
    get = function(e,id)
        if not e or (e and type(e) ~= "string") then
            error("bad argument #1 to 'get' (string expected)")
        end
        if not id or (id and type(id) ~= "string") then
            error("bad argument #2 to 'get' (string expected)")
        end
        if eventhandlers[e] then
            return eventhandlers[e][i]
        else
            return nil
        end
    end,
    gethandlers = function()
        return eventhandlers
    end,
    fire = function(e,...)
        if not e or (e and type(e) ~= "string") then
            error("bad argument #1 to 'fire' (string expected)")
        end
        eventhandlers[e] = eventhandlers[e] or {}
        local printstack = ""
        for _,handle in pairs(eventhandlers[e]) do
            local result = safefunc(nil,handle,...)
            local toadd = result.Success and result.PrintStack or result.Error
            printstack = printstack .. toadd .. "\n"
        end

        return printstack
    end,
}