import React from 'react';
import Row from 'react-bootstrap/lib/Row';
import Col from 'react-bootstrap/lib/Col';

export default class Home extends React.Component {
    displayName = Home.name;

    render() {
        return (
            <div className='home'>
                <video id='visualizer' src='./video/visualizer.mp4' autoPlay loop muted/>
                <div className='intro'>
                    <Row className='p-0 m-0'>
                        <Col md={2} className='p-0 m-0'/>
                        <Col md={8} className='p-0 m-0'>
                            <Row className='p-0 m-0'>
                                <Col md={6} className='p-0 m-0'>
                                    <img src='./img/logo.png' className='logo' alt='logo'/>
                                </Col>
                                <Col md={6} className='p-0 m-0'>
                                    <a href='https://discordapp.com/oauth2/authorize?client_id=360116713829695489&scope=bot&permissions=0' className='invite-btn'>Invite</a>
                                    <a href='./docs' className='learn-more-btn'>Learn more</a>
                                </Col>
                                <Col md={12} className='p-0 m-0'>
                                    <h2 className='intro-description'>An augmented Discord experience</h2>
                                </Col>
                            </Row>
                        </Col>
                        <Col md={2} className='p-0 m-0'/>
                    </Row>
                </div>
                <div className='getting-started'>
                    <h3>Under construction</h3><hr/><br/>
                    This website is currently being actively worked on.
                </div>
            </div>
        );
    }
}