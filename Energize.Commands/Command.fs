namespace Energize.Commands

open System

module Command =
    open Context

    type CommandModuleAttribute(name : string) =
        inherit Attribute()
        member val name : string = name

    type AdminOnlyCommandAttribute() =
        inherit Attribute()

    type NsfwCommandAttribute() = 
        inherit Attribute()

    type GuildOnlyCommandAttribute() =
        inherit Attribute()

    type OwnerOnlyCommandAttribute() = 
        inherit Attribute()    

    type CommandAttribute (name : string, help : string, usage : string) =
        inherit Attribute()
        member val name : string = name
        member val help : string = help
        member val usage : string = usage

    type CommandParametersAttribute(count : int) =
        inherit Attribute();
        member val parameters : int = count

    type CommandCallback = 
        delegate of CommandContext -> Async<unit>

    type Command =
        {
            name : string
            callback : CommandCallback
            mutable isEnabled : bool
            usage : string
            help : string
            moduleName : string
            parameters : int
            ownerOnly : bool
            guildOnly : bool
            isNsfw : bool
            adminOnly : bool
        }

