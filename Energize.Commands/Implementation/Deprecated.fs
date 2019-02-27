namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Deprecated")>]
module Deprecated =
    open Energize.Commands.Context

    //social
    [<Command("hug","deprecated, use act instead","deprecated")>]
    let hug (ctx : CommandContext) = async { ctx.sendWarn None "Use act instead" }

    [<Command("boop","deprecated, use act instead","deprecated")>]
    let boop (ctx : CommandContext) = async { ctx.sendWarn None "Use act instead" }

    [<Command("slap","deprecated, use act instead","deprecated")>]
    let slap (ctx : CommandContext) = async { ctx.sendWarn None "Use act instead" }

    [<Command("kiss","deprecated, use act instead","deprecated")>]
    let kiss (ctx : CommandContext) = async { ctx.sendWarn None "Use act instead" }

    [<Command("snuggle","deprecated, use act instead","deprecated")>]
    let snuggle (ctx : CommandContext) = async { ctx.sendWarn None "Use act instead" }

    [<Command("shoot","deprecated, use act instead","deprecated")>]
    let shoot (ctx : CommandContext) = async { ctx.sendWarn None "Use act instead" }
    
    [<Command("pet","deprecated, use act instead","deprecated")>]
    let pet (ctx : CommandContext) = async { ctx.sendWarn None "Use act instead" }
    
    [<Command("spank","deprecated, use act instead","deprecated")>]
    let spank (ctx : CommandContext) = async { ctx.sendWarn None "Use act instead" }
    
    [<Command("yiff","deprecated, use act instead","deprecated")>]
    let yiff (ctx : CommandContext) = async { ctx.sendWarn None "Use act instead" }

    [<Command("nom","deprecated, use act instead","deprecated")>]
    let nom (ctx : CommandContext) = async { ctx.sendWarn None "Use act instead" }

    [<Command("lick","deprecated, use act instead","deprecated")>]
    let lick (ctx : CommandContext) = async { ctx.sendWarn None "Use act instead" }

    [<Command("bite","deprecated, use act instead","deprecated")>]
    let bite (ctx : CommandContext) = async { ctx.sendWarn None "Use act instead" }



    // image processing
    [<Command("bw", "Under rewrite, WIP", "WIP")>]
    let bw (ctx : CommandContext) = async { ctx.sendWarn None "Under rewrite, WIP" }

    [<Command("jpg", "Under rewrite, WIP", "WIP")>]
    let jpg (ctx : CommandContext) = async { ctx.sendWarn None "Under rewrite, WIP" }

    [<Command("pixelate", "Under rewrite, WIP", "WIP")>]
    let pixelate (ctx : CommandContext) = async { ctx.sendWarn None "Under rewrite, WIP" }

    [<Command("invert", "Under rewrite, WIP", "WIP")>]
    let invert (ctx : CommandContext) = async { ctx.sendWarn None "Under rewrite, WIP" }

    [<Command("paint", "Under rewrite, WIP", "WIP")>]
    let paint (ctx : CommandContext) = async { ctx.sendWarn None "Under rewrite, WIP" }

    [<Command("intensify", "Under rewrite, WIP", "WIP")>]
    let intensify (ctx : CommandContext) = async { ctx.sendWarn None "Under rewrite, WIP" }

    [<Command("blur", "Under rewrite, WIP", "WIP")>]
    let blur (ctx : CommandContext) = async { ctx.sendWarn None "Under rewrite, WIP" }

    [<Command("greenify", "Under rewrite, WIP", "WIP")>]
    let greenify (ctx : CommandContext) = async { ctx.sendWarn None "Under rewrite, WIP" }

    [<Command("deepfry", "Under rewrite, WIP", "WIP")>]
    let deepfry (ctx : CommandContext) = async { ctx.sendWarn None "Under rewrite, WIP" }