import React from 'react';
import ReactDOM from 'react-dom';
import Summary from './Summary';
import Twemoji from 'react-twemoji';

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
        let title = <div><b>Conditions:</b><br /></div>;
        if (conditions.length > 0) {
            let list = conditions.map((cond, i) => <div key={'cond_' + i}>- {this.toCommandCondition(cond)}</div>);
            return <div>{title}{list}</div>;
        }

        return <div>{title}- No specific conditions required</div>;
    }

    formatPermissions(permissions) {
        let title = <div><b>Required permissions:</b><br /></div>;
        if (permissions.length > 0) {
            let list = permissions.map((perm, i) => <div key={'perm_' + i}>- {perm}</div>);
            list[list.length] = <div>- SendMessages</div>;
            return <div>{title}{list}</div>;
        }

        return <div>{title}- SendMessages</div>;
    }

    formatDescription(cmd) {
        return (
            <div>
                <div><b>Description:</b><br /></div>
                <div> {cmd.help}</div>
                <div><b> {cmd.parameters}</b> required arguments</div>
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

        return tags.map((tag, i) => (
            <div key={'tag_' + i} className='command'>
                <u><b>{tag.name}</b></u><br />
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
                try {
                    let cmdInfo = await response.json();
                    this.commands = cmdInfo.commands;
                    this.prefix = cmdInfo.prefix;
                    this.botMention = cmdInfo.botMention;
                } catch (err) {
                    console.debug(err);
                }
            }
        }

        if (this.commands.length > 0) {
            let info = <div>To use the below commands you can either use the prefix (<b>{this.prefix}</b>), either mention the bot (<b>@{this.botMention}</b>).</div>;
            let elements = this.commands;
            if (search !== null) {
                search = search.toLowerCase();
                elements = elements.filter(cmd => cmd.name.includes(search) || cmd.moduleName.toLowerCase().includes(search));
                let result = <span><b>{elements.length}</b> commands found.</span>;
                ReactDOM.render(result, document.getElementById('searchResult'));
            } else {
                ReactDOM.render(<span/>, document.getElementById('searchResult'));
            }

            elements = elements.map((cmd,i) => (
                <div key={'cmd_' + i} className='command'>
                    <u><b>{cmd.name}</b>  [<i>{cmd.moduleName}</i>]</u>
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

    onSummaryClick = (e) => {
        e.preventDefault();
        let element = e.target;
        let id = element.id.substring(4);
        let targetElement = document.getElementById(id);
        window.scroll({
            top: targetElement.offsetTop,
            behavior: 'smooth'
        })
    }

    onFabTopclick = (e) => {
        window.scroll({
            top: 0,
            behavior: 'smooth'
        })
    }

    render() {
        this.fetchCommands(null);

        return (
            <div>
                <Summary>
                    <span><a id='sum-description' href='docs#description' onClick={this.onSummaryClick}>Description</a></span>
                    <span>
                        <a id='sum-commands' href='docs#commands' onClick={this.onSummaryClick}>Commands</a>
                        <span>
                            <a id='sum-updating-cmd-msg' href='docs#updating-cmd-msg' onClick={this.onSummaryClick}>
                                Updating a command message
                            </a>
                        </span>
                        <span>
                            <a id='sum-deleting-cmd-msg' href='docs#deleting-cmd-msg' onClick={this.onSummaryClick}>
                                Deleting a command message
                            </a>
                        </span>
                        <span>
                            <a id='sum-paginated-cmd-results' href='docs#paginated-cmd-results' onClick={this.onSummaryClick}>
                                Paginated command results
                            </a>
                        </span>
                        <span>
                            <a id='sum-cmd-user-input' href='docs#cmd-user-input' onClick={this.onSummaryClick}>
                                Ways to target users in commands
                            </a>
                        </span>
                        <span>
                            <a id='sum-target-user-tags' href='docs#target-user-tags' onClick={this.onSummaryClick}>
                                Targetting users with tags
                            </a>
                        </span>
                    </span>
                    <span>
                        <a id='sum-playable-messages' href='docs#playable-messages' onClick={this.onSummaryClick}>
                            Playable messages
                        </a>
                    </span>
                </Summary>

                <button className='fabTop' onClick={this.onFabTopclick}>⇪</button>

                <h2>Documentation</h2>
                <h4><i>Here you will find documentation for Energize various commands and features.</i></h4>
                <h3 id='description'>Description</h3>
                <p>
                    Energize primary feature is <b>music</b>, streaming music <b>through a discord audio channel</b> more specifically.
                    It can stream a large variety of sources including <b>Youtube, Twitch, SoundCloud, Vimeo and more</b>.<br />
                    Along with music features, there are some <b>moderation, NSFW and social</b> features. Energize aims to be <b>simple of use</b> for the average user but also to provide
                    a <b>good amount of features</b> to satisfy even the Discord's veterans.
                </p>

                <h3 id='commands'>Commands</h3>
                <input type='text' onChange={this.onSearch} placeholder='search commands...' /> <span id='searchResult' />
                <div id='commandRoot'>Generating commands documentation...</div>

                <h4 id='paginated-cmd-results'>Paginated command results</h4>
                Sometimes when using a command with the bot, you will get results that have reactions on them. There are usually <b>3 or 4</b> added
                reactions to these.<br/><br/>Here is an example: <br/>
                <img src='./img/docs/paginated_result_1.png' alt='paginated result example' className='content-img' /><br/>
                Each reaction added by the bot corresponds to a <b>different available action</b>. In the case of paginated results it goes as follows:<br/>
                <Twemoji options={{className: 'twemoji'}}>
                    - The ◀ reaction will load the content of the <b>previous page</b> of the result.<br/>
                    - The ⏹ reaction will <b>delete</b> the result.<br/>
                    - The ⏯ reaction will <b>add the current page result to the track queue</b>, note that this is only available on paginated track results.<br/>
                    - The ▶ reaction will load the content of the <b>next page</b> of the result.<br/>
                </Twemoji><br/>

                <u><b>Paginated results behaviors:</b></u><br/>
                - If you stopped using the paginated message during 5 minutes the bot will <b>not</b> react to any of your reactions anymore.<br/>
                - Only the command author can use the paginated result reactions.<br/><br/>

                <h4 id='cmd-user-input'>Ways to target users in commands</h4>
                Some commands require you to pass a user as argument, there are a few ways to feed the bot a user.
                Here are the several ways this can be achieved:<br/><br/>
                <ol>
                    <li>Typing the name (nickname in a guild) of the user.</li>
                    <li>Mentioning the user.</li>
                    <li>Using built-in tagging system.</li>
                </ol><br/>

                <h4 id='target-user-tags'>Targetting users with tags</h4>
                As mentioned before, the bot features a <b>built-in tagging system</b>, it means that there is an existing
                syntax to tag a wanted user. There are currently <b>4</b> usable built-in tags. You can use the tags by prefixing
                one of the existing tags with the <b>$</b> character like so as an argument in commands that require user arguments.<br/><br/>
                Here is an example:<br/>
                <code>cmd $random,$last</code><br/><br/>
                {this.formatBuiltInTags()}

                <h3 id='playable-messages'>Playable messages</h3>
                <Twemoji options={{className: 'twemoji'}}>
                    If the bot has the permissions necessary you maybe have noticed that some messages get a ⏯ reaction. This means that
                    those messages have content that can be added to the track queue. Although this will only work if you are in a voice channel.
                    Usually messages that can be "played" are messages containing Youtube, SoundCloud and Twitch content.
                </Twemoji><br/>

                Example:<br/>
                <img src='./img/docs/playable_message.png' alt='playable message example' className='content-img'/><br/>
            </div>
            );
    }
}
