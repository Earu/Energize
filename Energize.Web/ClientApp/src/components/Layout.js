import React from 'react';
import Row from 'react-bootstrap/lib/Row';
import Col from 'react-bootstrap/lib/Col';

import Header from './Header';
import Menu from './Menu';

export default class Layout extends React.Component {
    displayName = Layout.name;

    render() {
        return (
            <div>
                <Header/>
                <div className='container-fluid'>
                    <Row>
                        <Col md={12} className='spacer' />
                        <Col md={1} className='menu'>
                            <Menu />
                        </Col>
                        <Col md={11}>
                            <div className='content'>{this.props.children}</div>
                        </Col>
                    </Row>
                </div>
            </div>
        );
    }
}
