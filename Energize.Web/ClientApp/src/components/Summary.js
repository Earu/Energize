import React from 'react';

export default class Summary extends React.Component {
    displayName = Summary.name;

    render() {
        return (
            <div className='summary'>
                <div>Summary</div>
                {this.props.children}
            </div>
        );
    }
}