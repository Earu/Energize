namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Deprecated")>]
module Deprecated =
    open Energize.Commands.Context

    //social
    [<Command("hug","Deprecated use act instead","deprecated")>]
    let hug (ctx : CommandContext) = async { return [ ctx.sendWarn None "Deprecated, use act instead" ] }

    [<Command("boop","Deprecated use act instead","deprecated")>]
    let boop (ctx : CommandContext) = async { return [ ctx.sendWarn None "Deprecated, use act instead" ] }

    [<Command("slap","Deprecated use act instead","deprecated")>]
    let slap (ctx : CommandContext) = async { return [ ctx.sendWarn None "Deprecated, use act instead" ] }

    [<Command("kiss","Deprecated use act instead","deprecated")>]
    let kiss (ctx : CommandContext) = async { return [ ctx.sendWarn None "Deprecated, use act instead" ] }

    [<Command("snuggle","Deprecated use act instead","deprecated")>]
    let snuggle (ctx : CommandContext) = async { return [ ctx.sendWarn None "Deprecated, use act instead" ] }

    [<Command("shoot","Deprecated use act instead","deprecated")>]
    let shoot (ctx : CommandContext) = async { return [ ctx.sendWarn None "Deprecated, use act instead" ] }
    
    [<Command("pet","Deprecated use act instead","deprecated")>]
    let pet (ctx : CommandContext) = async { return [ ctx.sendWarn None "Deprecated, use act instead" ] }
    
    [<Command("spank","Deprecated use act instead","deprecated")>]
    let spank (ctx : CommandContext) = async { return [ ctx.sendWarn None "Deprecated, use act instead" ] }
    
    [<Command("yiff","Deprecated use act instead","deprecated")>]
    let yiff (ctx : CommandContext) = async { return [ ctx.sendWarn None "Deprecated, use act instead" ] }

    [<Command("nom","Deprecated use act instead","deprecated")>]
    let nom (ctx : CommandContext) = async { return [ ctx.sendWarn None "Deprecated, use act instead" ] }

    [<Command("lick","Deprecated use act instead","deprecated")>]
    let lick (ctx : CommandContext) = async { return [ ctx.sendWarn None "Deprecated, use act instead" ] }

    [<Command("bite","Deprecated use act instead","deprecated")>]
    let bite (ctx : CommandContext) = async { return [ ctx.sendWarn None "Deprecated, use act instead" ] }

    // image processing
    [<Command("bw", "Under rewrite, WIP", "WIP")>]
    let bw (ctx : CommandContext) = async { return [ ctx.sendWarn None "Under rewrite, WIP" ] }

    [<Command("jpg", "Under rewrite, WIP", "WIP")>]
    let jpg (ctx : CommandContext) = async { return [ ctx.sendWarn None "Under rewrite, WIP" ] }

    [<Command("pixelate", "Under rewrite, WIP", "WIP")>]
    let pixelate (ctx : CommandContext) = async { return [ ctx.sendWarn None "Under rewrite, WIP" ] }

    [<Command("invert", "Under rewrite, WIP", "WIP")>]
    let invert (ctx : CommandContext) = async { return [ ctx.sendWarn None "Under rewrite, WIP" ] }

    [<Command("paint", "Under rewrite, WIP", "WIP")>]
    let paint (ctx : CommandContext) = async { return [ ctx.sendWarn None "Under rewrite, WIP" ] }

    [<Command("intensify", "Under rewrite, WIP", "WIP")>]
    let intensify (ctx : CommandContext) = async { return [ ctx.sendWarn None "Under rewrite, WIP" ] }

    [<Command("blur", "Under rewrite, WIP", "WIP")>]
    let blur (ctx : CommandContext) = async { return [ ctx.sendWarn None "Under rewrite, WIP" ] }

    [<Command("greenify", "Under rewrite, WIP", "WIP")>]
    let greenify (ctx : CommandContext) = async { return [ ctx.sendWarn None "Under rewrite, WIP" ] }

    [<Command("deepfry", "Under rewrite, WIP", "WIP")>]
    let deepfry (ctx : CommandContext) = async { return [ ctx.sendWarn None "Under rewrite, WIP" ] }

    // name changes
    [<Command("furrybooru", "Use furb command instead", "use furb instead")>]
    let furb (ctx : CommandContext) = async { return [ ctx.sendWarn None "Use furb instead" ] }

    [<Command("gelbooru", "Use gelb command instead", "use gelb instead")>]
    let gelb (ctx : CommandContext) = async { return [ ctx.sendWarn None "Use gelb instead" ] }