import React from 'react';

export default class Header extends React.Component
{
    displayName = Header.name;

    render()
    {
        return (
            <header>
                <img  src='./img/logo.png' alt='logo' />
                <h1>Energize</h1>
            </header>
        );
    }
}
