prefix = '*'
commands = {
     yes = {
          callback = function(user) print(user.Mention, ' what are you fucking gay?') end
     }
}
event.attach('OnMessageCreated', 'a', function(user, msg)
     local str = msg.Content
     local _prefix = string.sub(str, 0, #prefix)
     if _prefix ~= prefix then return end

     local cmd = string.sub(str, #_prefix + 1, #str)

     if commands[cmd] and commands[cmd].callback then
          commands[cmd].callback(user, str)
     end
end) -- shitcode
-- GEN --
prefix = '*'
commands = {
     yes = {
          callback = function(user) print(user.Mention, 'what are you fucking gay?') end
     }
}
event.attach('OnMessageCreated', 'a', function(user, msg)
     local str = msg.Content
     local _prefix = string.sub(str, 0, #prefix)
     if _prefix ~= prefix then return end

     local cmd = string.sub(str, #_prefix + 1, #str)

     if commands[cmd] and commands[cmd].callback then
          commands[cmd].callback(user, str)
     end
end) -- shitcode
-- GEN --
return collectgarbage
-- GEN --
print(math.random(1,2) == 1 and "vanilla" or "chocolate")
-- GEN --
