[![pipeline status](https://gitlab.com/Earu/energize/badges/master/pipeline.svg)](https://gitlab.com/Earu/energize/commits/master)

Energize⚡ is a robust **music** / **administration** / **social** bot featuring well-crafted music search *(Youtube, SoundCloud, Twitch, Stream urls)*, user friendly, it proposes a set of useful commands.

[![Discord Bots](https://discordbots.org/api/widget/360116713829695489.svg)](https://discordbots.org/bot/360116713829695489)
<img src="https://dl.dropboxusercontent.com/s/8k0lwukl9n1shki/new_attempt_2.png" width="25%">

Join our support server to learn more about Energize: https://discord.gg/RXZtBr5

### Structure
Almost everything in Energize is a service, services are implemented under the **Energize** project under [Services](https://github.com/Earu/Energize/tree/master/Energize/Services). Because the command service is in F# it has its own project (**Energize.Commands**), and because it needed to interface with other projects **Energize.Interfaces** was born, it contains interfaces for services. Finally **Energize.Essentials** contains essential classes used in the other projects; see it as a toolbox. 

### Where are the commands ?
You can find them [here](https://github.com/Earu/Energize/tree/master/Energize.Commands/Implementation).

### Why use F#?
F# works using modules and is rather powerful when coming to computations that do not require having lots of different structures which made it perfect in some of the parts of Energize, essentially the command implementation.

### I need help!
If you are lost in how to use Energize, or how to make it work, feel free to open an issue with the tag `[ HELP ]`. You can also add me (Earu Arcana#9037) or use the `feedback` and/or `bug` commands on Discord.

### Contributing
If you are willing to contribute to Energize, here are a few things that needs to be done to stay consistent with the rest of the code:
- C# projects have: local variables in *camelCase*, methods, properties and fields in *PascalCase*.
- F# projects have: methods available for the whole solution in *PascalCase*, the rest is *camelCased*.
- If you create a new service do **not** create interfaces in Energize.Interfaces unless they are used in other projects.
- The bot purpose is focused on the music, administration and social areas, try to keep this in mind if you want to add new commands.
- Try to use the *interfaces* proposed by Discord.NET and other packages as much as possible in your method signatures.
