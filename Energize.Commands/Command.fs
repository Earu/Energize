namespace Energize.Commands

module Command =
    open Context

    type Command =
        {
            name : string
            callback : (CommandContext -> Async<unit>)
            mutable isLoaded : bool
            usage : string
            help : string
            moduleName : string
            parameters : int
        }

