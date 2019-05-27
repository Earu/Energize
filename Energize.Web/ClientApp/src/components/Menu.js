import React from 'react';
import { NavLink } from 'react-router-dom';

export default class Menu extends React.Component {
    displayName = Menu.name;

    render() {
        return (
            <div className='links'>
                <div><NavLink to="/" exact>Back to Home</NavLink></div>
                <hr />
                <div><NavLink to="/docs">Docs</NavLink></div>
                <div><NavLink to="/music">Music</NavLink></div>
                <div><NavLink to="/admin">Admin</NavLink></div>
                <div><NavLink to="/stats">Stats</NavLink></div>
            </div>
        );
    }
}
