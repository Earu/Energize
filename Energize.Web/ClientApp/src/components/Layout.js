import React from 'react';
import Header from './Header';
import Menu from './Menu';

export default class Layout extends React.Component {
    displayName = Layout.name;

    render() {
        return (
            <div>
                <Header />
                <div className="container-fluid">
                    <div className="row">
                        <div className="col-md-12 spacer"/>
                        <div className="col-md-1 menu"><Menu /></div>
                        <div className="col-md-11">
                            {this.props.children}
                        </div>
                    </div>
                </div>
            </div>
        );
    }
}
