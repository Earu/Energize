namespace Energize.Commands

open System
open Context
open Discord

module Command =
    type CommandModuleAttribute(name : string) =
        inherit Attribute()
        member val name : string = name

    type CommandAttribute (name : string, help : string, usage : string) =
        inherit Attribute()
        member val name : string = name
        member val help : string = help
        member val usage : string = usage

    type CommandCondition =
    | AdminOnly = 0
    | NsfwOnly = 1
    | GuildOnly = 2
    | DevOnly = 3

    type CommandConditionsAttribute([<ParamArray>] conditions : CommandCondition array) =
        inherit Attribute()
        member val conditions : CommandCondition list = conditions |> Array.toList

    type CommandParametersAttribute(count : int) =
        inherit Attribute()
        member val parameters : int = count

    type CommandPermissionsAttribute([<ParamArray>] perms : ChannelPermission array) =
        inherit Attribute()
        member val permissions : ChannelPermission list = perms |> Array.toList

    type MaintenanceFreeCommand() =
        inherit Attribute()

    type CommandCallback = 
        delegate of CommandContext -> Async<IUserMessage list>

    type Command =
        {
            name : string
            callback : CommandCallback
            mutable isEnabled : bool
            usage : string
            help : string
            moduleName : string
            parameters : int
            permissions : ChannelPermission list
            conditions : CommandCondition list
            maintenanceFree : bool
        }

