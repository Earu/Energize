
[![Discord Bots](https://discordbots.org/api/widget/360116713829695489.svg)](https://discordbots.org/bot/360116713829695489)
<img src="https://dl.dropboxusercontent.com/s/8k0lwukl9n1shki/new_attempt_2.png" width="25%">

# Energize, multi-usage Discord bot

Source for Energize⚡ a bot using Discord.NET. Image processing, text generation, music player, administration and more. 

## Everything in Energize is a service.

The implementation of commands and services are my own.

## Where are the command implementations ?

You can find them [here](https://github.com/Earu/Energize/tree/master/Energize.Commands/Implementation)

## Uhhh, where is the C# code?

The notion of modules felt more appropriate regarding commands, usually when you create a command you create a method
in C# that method would need to be used from an instance of an object, which is not really what it is here. However F# has modules where you
can have functions that do not have to be part of an instance. That feels more appropriate for a command system, so commands are in F# and not C#!
