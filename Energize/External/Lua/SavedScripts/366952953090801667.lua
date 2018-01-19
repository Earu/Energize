return "A" .. "R" .. "F"
-- GEN --
event.attach("OnMessageCreated","test",function(user,msg) if not user.Id == 97436844198727680 then return end msg.DeleteAsync() end)
-- GEN --
printtable(event)
-- GEN --
