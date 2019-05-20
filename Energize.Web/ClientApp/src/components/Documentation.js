import React from 'react';
import ReactDOM from 'react-dom';

export default class Menu extends React.Component {
    displayName = Menu.name;
    commands = [];
    prefix = '';
    botMention = '';

    toCommandCondition(n) {
        switch (n) {
            case 0:
                return 'Can only be used by admins or in DM';
            case 1:
                return 'Can only be used in a NSFW channel or in DM';
            case 2:
                return 'Can only be used in a server';
            case 3:
                return 'Can only be used by the owner of the bot';
            default:
                return 'Unknown condition ?';
        }
    }

    formatConditions(conditions) {
        let title = <div><strong>Conditions:</strong><br /></div>;
        if (conditions.length > 0) {
            let list = conditions.map(cond => <div>- {this.toCommandCondition(cond)}</div>);
            return <div>{title}{list}</div>;
        }

        return <div>{title}- No specific conditions required</div>;
    }

    formatPermissions(permissions) {
        let title = <div><strong>Required permissions:</strong><br /></div>;
        if (permissions.length > 0) {
            let list = permissions.map(perm => <div>- {perm}</div>);
            list[list.length] = <div>- SendMessages</div>;
            return <div>{title}{list}</div>;
        }

        return <div>{title}- SendMessages</div>;
    }

    formatDescription(cmd) {
        return (
            <div>
                <div><strong>Description:</strong><br /></div>
                <div> {cmd.help}</div>
                <div><strong> {cmd.parameters}</strong> required arguments</div>
                <code> {cmd.usage}</code>
            </div>
        );
    }

    formatBuiltInTags() {
        let tags = [
            {
                name: 'me',
                description: 'Targets yourself'
            },
            {
                name: 'random',
                description: 'Targets a random user on the server'
            },
            {
                name: 'admin',
                description: 'Targets a random admin on the server'
            },
            {
                name: 'last',
                description: 'Targets the last user who has spoken in the channel'
            }
        ]

        return tags.map(tag => (
            <div className="command">
                <u><strong>{tag.name}</strong></u><br />
                {tag.description}
            </div>
        ));
    }

    async fetchCommands(search) {
        if (this.commands.length === 0) {
            let response = await fetch('./api/commands', {
                method: 'GET'
            });

            if (response.ok) {
                let cmdInfo = await response.json();
                this.commands = cmdInfo.commands;
                this.prefix = cmdInfo.prefix;
                this.botMention = cmdInfo.botMention;
            }
        }

        if (this.commands.length > 0) {
            let info = <div>To use the below commands you can either use the prefix (<strong>{this.prefix}</strong>), either mention the bot (<strong>@{this.botMention}</strong>).</div>;
            let elements = this.commands;
            if (search !== null) {
                search = search.toLowerCase();
                elements = elements.filter(cmd => cmd.name.includes(search) || cmd.moduleName.toLowerCase().includes(search));
                let result = <span><strong>{elements.length}</strong> commands found.</span>;
                ReactDOM.render(result, document.getElementById('searchResult'));
            }
            else {
                ReactDOM.render(<span/>, document.getElementById('searchResult'));
            }

            elements = elements.map(cmd => (
                <div className="command">
                    <u><strong>{cmd.name}</strong>  [<i>{cmd.moduleName}</i>]</u>
                    <br /><br />
                    {this.formatDescription(cmd)}<br/>
                    {this.formatConditions(cmd.conditions)}<br />
                    {this.formatPermissions(cmd.permissions)}
                </div>
            ));
            ReactDOM.render(<div>{info}<br />{elements}</div>, document.getElementById('commandRoot'));
        } else {
            ReactDOM.render(<div style={{ color: 'orange' }}>Failed to generate command documentation</div>, document.getElementById('commandRoot'));
        }
    }

    onSearch = (e) => {
        let search = e.target.value;
        if (search === '')
            search = null;

        if (this.commands.length > 0)
            this.fetchCommands(search);
    }

    render() {
        this.fetchCommands(null);
        return (
            <div>
                <h2>Documentation</h2>
                <h4><i>Here you will find documentation for Energize various commands and features.</i></h4> 
                <h3>Summary</h3>
                <ul>
                    <li><h4><a href="#purpose">Purpose</a></h4></li>
                    <li>
                        <h4><a href="#commands">Commands</a></h4>
                        <ul>
                            <li><h5><a href="#cmd-user-input">Using commands targetting user(s)</a></h5></li>
                            <li><h5><a href="#target-user-tags">Targetting users with tags</a></h5></li>
                        </ul>
                    </li>
                </ul>
                <h3 id="purpose">Purpose</h3>
                <p>
                    Energize primary feature is <strong>music</strong>, streaming music <strong>through a discord audio channel</strong> more specifically.
                    It can stream a large variety of sources including <strong>Youtube, Twitch, SoundCloud, Vimeo and more</strong>.<br />
                    Along with music features, there are some <strong>moderation, NSFW and social</strong> features. Energize aims to be <strong>simple of use</strong> for the average user but also to provide
                    a <strong>good amount of features</strong> to satisfy even the Discord's veterans.
                </p>
                <h3 id="commands">Commands</h3>
                <input type="text" onChange={this.onSearch} placeholder="search commands..." /> <span id="searchResult" />
                <div id="commandRoot">Generating commands documentation...</div>
                <h4 id="cmd-user-input">Using commands targetting user(s)</h4>
                Some commands require you to pass a user as argument, there are a few ways to feed the bot a user.
                Here are the several ways this can be achieved:<br/><br/>
                <ol>
                    <li>Typing the name (nickname in a guild) of the user.</li>
                    <li>Mentioning the user.</li>
                    <li>Using built-in tagging system.</li>
                </ol><br/>
                <h4 id="target-user-tags">Targetting users with tags</h4>
                As mentioned before, the bot features a <strong>built-in tagging system</strong>, it means that there is an existing
                syntax to tag a wanted user. There are currently <strong>4</strong> usable built-in tags. You can use the tags by prefixing
                one of the existing tags with the <strong>$</strong> character like so as an argument in commands that require user arguments.<br/><br/>
                Here is an example:<br/>
                <code>cmd $random,$last</code><br/><br/>
                {this.formatBuiltInTags()}
            </div>
            );
    }
}
