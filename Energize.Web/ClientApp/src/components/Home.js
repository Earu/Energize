import React from 'react';
import ReactDOM from 'react-dom';
import Row from 'react-bootstrap/lib/Row';
import Col from 'react-bootstrap/lib/Col';

export default class Home extends React.Component {
    displayName = Home.name;

    capitalize(string) {
        return string.charAt(0).toUpperCase() + string.slice(1);
    }

    async fetchAndDisplayChangelog()
    {
        let response = await fetch('https://api.github.com/repos/Earu/Energize/commits', {
            method: 'GET'
        });

        if (response.ok)
        {
            let commits = await response.json();
            commits = commits.map(commit => (
                <li className='commit'>
                    <a href={commit.html_url}>
                        {this.capitalize(commit.commit.message)}
                        <img src={commit.author.avatar_url} alt='author_avatar'/>
                        <span>{commit.author.login}</span>
                    </a>
                </li>
            ));
            console.debug(commits);
            ReactDOM.render(<ul>{commits}</ul>, document.getElementById('changelogRoot'));
        }
    }

    async fetchAndDisplayBotInformation() {
        let response = await fetch('./api/info', {
            method: 'GET'
        });

        if (response.ok) {
            let botInfo = await response.json();
            ReactDOM.render(botInfo.userCount, document.getElementById('userCount'))
            ReactDOM.render(botInfo.serverCount, document.getElementById('serverCount'))
        }
    }

    render() {
        this.fetchAndDisplayChangelog();
        this.fetchAndDisplayBotInformation();
        return (
            <div className='home'>
                <video id='visualizer' src='./video/visualizer.mp4' autoPlay loop muted />
                <div className='intro'>
                    <Row className='p-0 m-0'>
                        <Col md={2} className='p-0 m-0' />
                        <Col md={8} className='p-0 m-0'>
                            <Row className='p-0 m-0'>
                                <Col md={6} className='p-0 m-0'>
                                    <img src='./img/logo.png' className='logo' alt='logo' />
                                </Col>
                                <Col md={6} className='p-0 m-0'>
                                    <a href='https://discordapp.com/oauth2/authorize?client_id=360116713829695489&scope=bot&permissions=0' className='invite-btn'>Invite</a>
                                    <a href='./docs' className='learn-more-btn'>Learn more</a>
                                </Col>
                                <Col md={12} className='p-0 m-0'>
                                    <h2 className='intro-description'>An augmented Discord™ experience</h2>
                                </Col>
                            </Row>
                        </Col>
                        <Col md={2} className='p-0 m-0' />
                    </Row>
                </div>
                <div className='pros-container'>
                    <Row className='pros container'>
                        <Col md={4}>
                            <div>
                                <i className='fas fa-broadcast-tower' />
                                <br />
                                <span>Radios</span>
                                <hr />
                                <p>Take advantage of a large number of radio stations.</p>
                            </div>
                        </Col>
                        <Col md={4}>
                            <div>
                                <i className='fab fa-youtube' />
                                <br />
                                <span>Diverse Sources</span>
                                <hr />
                                <p>Search and play songs from your favorite websites, including Spotify!</p>
                            </div>
                        </Col>
                        <Col md={4}>
                            <div>
                                <i className='fas fa-user-tie' />
                                <br />
                                <span>User Friendly</span>
                                <hr />
                                <p>Enjoy Energize features like a Discord pro.</p>
                            </div>
                        </Col>
                        <Col md={4}>
                            <div>
                                <i className='fas fa-chart-bar' />
                                <br />
                                <span>Uptime</span>
                                <hr />
                                <p>Energize is accessible at any moment as long as you have a connected Discord account.</p>
                            </div>
                        </Col>
                        <Col md={4}>
                            <div>
                                <i className='fas fa-terminal' />
                                <br />
                                <span>Commands</span>
                                <hr />
                                <p>A large number of commands to empower our users.</p>
                            </div>
                        </Col>
                        <Col md={4}>
                            <div>
                                <i className='fas fa-envelope-open-text' />
                                <br />
                                <span>Support</span>
                                <hr />
                                <p>Send us your bugs and ideas and expect a quick answer!</p>
                            </div>
                        </Col>
                    </Row>
                </div>
                <div className='stats' style={{backgroundImage: 'url(./img/mixer.png)'}}>
                    <br /><h3 style={{ margin: 0 }}>Facts about Energize⚡</h3>
                    <div className='container'>
                        <Row>
                            <Col md={4}>
                                <div className='stat'>
                                    <i className="fas fa-users" /><br /><span id='serverCount'>0</span> Servers
                                    </div>
                            </Col>
                            <Col md={4}>
                                <div className='stat'>
                                    <i className="fas fa-user" /><br /><span id='userCount'>0</span> Users
                                    </div>
                                </Col>
                            <Col md={4}>
                                <div className='stat'>
                                    <i className="fas fa-code" /><br /> 20K+ lines of code
                                </div>
                            </Col>
                        </Row>
                    </div>
                </div>
                <div className='changelog'>
                    <div className='container'>
                        <h3>Changelog</h3>
                        <ul id='changelogRoot'/>
                    </div>
                </div>
            </div>
        );
    }
}