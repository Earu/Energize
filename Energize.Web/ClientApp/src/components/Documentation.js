import React from 'react';

export default class Menu extends React.Component {
    displayName = Menu.name;
    commandElements = <div></div>;

    async fetchCommands()
    {
        let response = await fetch("./api/commands", {
                method: 'GET'
            });

        let cmds = await response.json();
        this.commandElements = cmds.map(cmd => <strong>{cmd.name}</strong>);
    }

    render() {
        this.fetchCommands();
        return (
            <div>
                <h2>Documentation</h2>
                <h4>Here you will find documentation for Energize various commands and features.</h4> 
                <br />
                <p>
                    Energize primary feature is <strong>music</strong>, streaming music <strong>through a discord audio channel</strong> more specifically.
                    It can stream a large variety of sources including <strong>Youtube, Twitch, SoundCloud, Vimeo and more</strong>.<br />
                    Along with music features, there are some <strong>moderation, NSFW and social</strong> features.<br />
                </p>
                <h3>Commands</h3>
                {this.commandElements}
            </div>
            );
    }
}
