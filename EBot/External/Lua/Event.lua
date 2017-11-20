event = {
    handlers = {},
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
        event.handlers[e] = event.handlers[e] or {}
        event.handlers[e][id] = handler
    end,
    detach = function(e,id)
        if not e or (e and type(e) ~= "string") then
            error("bad argument #1 to 'detach' (string expected)")
        end
        if not id or (id and type(id) ~= "string") then
            error("bad argument #2 to 'detach' (string expected)")
        end
        if event.handlers[e] then
            event.handlers[e][id] = nil
        end
    end,
    get = function(e,id)
        if not e or (e and type(e) ~= "string") then
            error("bad argument #1 to 'get' (string expected)")
        end
        if not id or (id and type(id) ~= "string") then
            error("bad argument #2 to 'get' (string expected)")
        end
        if event.handlers[e] then
            return event.handlers[e][i]
        else
            return nil
        end
    end,
    gethandlers = function()
        return event.handlers
    end,
    fire = function(e,...)
        if not e or (e and type(e) ~= "string") then
            error("bad argument #1 to 'fire' (string expected)")
        end
        event.handlers[e] = event.handlers[e] or {}
        local printstack = ""
        for _,handle in pairs(event.handlers[e]) do
            local result = safefunc(nil,handle,...)
            local toadd = result.Success and result.PrintStack or result.Error
            printstack = printstack .. toadd .. "\n"
        end

        return printstack
    end,
}