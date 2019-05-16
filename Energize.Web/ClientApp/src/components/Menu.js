import React from 'react';
import { NavLink } from 'react-router-dom';

export default class Menu extends React.Component {
    displayName = Menu.name;

    onClick(e) {
        alert('epic');
    }

    render() {
        return (
            <ul className="links">
                <li><NavLink to="/docs" activeStyle={{ color: '#A51C15' }}>Docs</NavLink></li>
                <li><NavLink to="/music" activeStyle={{ color: '#A51C15' }}>Music</NavLink></li>
                <li><NavLink to="/admin" activeStyle={{ color: '#A51C15' }}>Admin</NavLink></li>
                <li><NavLink to="/stats" activeStyle={{ color: '#A51C15' }}>stats</NavLink></li>
                <li>Other</li>
                <li>Credits</li>
            </ul>
        );
    }
}
