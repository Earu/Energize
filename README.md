
[![Discord Bots](https://discordbots.org/api/widget/360116713829695489.svg)](https://discordbots.org/bot/360116713829695489)
<img src="https://dl.dropboxusercontent.com/s/8k0lwukl9n1shki/new_attempt_2.png" width="25%">

Energize⚡ is a robust **music** / **administration** / **social** bot featuring well-crafted music search *(Youtube, SoundCloud, Twitch, Stream urls)*, user friendly, it proposes a set of useful commands.

## Structure
Almost everything in Energize is a service, services are implemented under the **Energize** project under [Services](https://github.com/Earu/Energize/tree/master/Energize/Services). Because the command service and the markov service are in F# they have their own projects (**Energize.Commands & Energize.Markov**), and because they needed to interface with other projects **Energize.Interfaces** was born, it contains interfaces for services. **Energize.Essentials** contains essential classes used in the other projects see it as a toolkit. Finally **Energize.Webhooks** is a small python webserver made with Flask that is used to post messages in a channel via a webhook when it receives a post request.

## Where are the commands ?
You can find them [here](https://github.com/Earu/Energize/tree/master/Energize.Commands/Implementation).

## Why use F#?
F# works using modules and is rather powerful when coming to computations that do not require having lots of different structures which made it perfect in some of the parts of Energize, essentially the command implementation and the markov algorithm. 
