import React from 'react';
import ReactDOM from 'react-dom';

export default class Menu extends React.Component {
    displayName = Menu.name;

    async fetchCommands()
    {
        let response = await fetch('./api/commands', {
                method: 'GET'
            });

        let cmds = await response.json();
        let elements = cmds.map(cmd => (
            <div className="command">
                <strong>{cmd.name}</strong><br/>
                <p>{cmd.help}</p>
                <code>{cmd.usage}</code>
            </div>
            ));
        ReactDOM.render(<div>{elements}</div>, document.getElementById('commandRoot'));
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
                <div id="commandRoot">Generating commands documentation...</div>
            </div>
            );
    }
}
