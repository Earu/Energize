local istype = function(what,t)
    if not what or (what and type(what) ~= t) then
        return false
    else
        return true
    end
end

event = {}

event.handlers = {}

event.attach = function(e,id,handle)
    if not istype(e,"string") then return end
    if not istype(id,"string") then return end
    if not istype(handle,"function") then return end

    event.handlers[e] = event.handlers[e] or {}
    event.handlers[e][id] = handle
end
    
event.detach = function(e,id)
    if not istype(e,"string") then return end
    if not istype(id,"string") then return end

    if event.handlers[e] then
        event.handlers[e][id] = nil
    end
end
    
event.get = function(e,id)
    if not istype(e,"string") then return end
    if not istype(id,"string") then return end

    if event.handlers[e] then
        return event.handlers[e][i]
    else
        return nil
    end
end

event.fire = function(e,...)
    if not istype(e,"string") then return end

    event.handlers[e] = event.handlers[e] or {}
    local printstack = ""
    for _,handle in pairs(event.handlers[e]) do
        local result = safefunc(nil,handle,...)
        local toadd = result.Success and result.PrintStack or result.Error
        printstack = printstack .. toadd .. "\n"
    end

    return printstack
end