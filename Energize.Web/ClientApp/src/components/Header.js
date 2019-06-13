import React from 'react';

export default class Header extends React.Component {
    displayName = Header.name;

    constructor() {
        super();

        window.addEventListener('resize', (e) => {
            let menu = document.getElementsByClassName('menu')[0];
            if (window.innerWidth <= 767)
                menu.style.display = 'none';
            else
                menu.style.display = 'block';
        });
    }

    onCheeseburgerClick = (e) => {
        let menu = document.getElementsByClassName('menu')[0];
        if (menu.style.display === 'none' || menu.style.display === '')
            menu.style.display = 'block';
        else
            menu.style.display = 'none';
    }

    render() {
        return (
            <header>
                <img src='./img/logo_white.png' alt='logo' />
                <h1>Energize</h1>
                <button onClick={this.onCheeseburgerClick}><i className='fas fa-bars' /></button>
            </header>
        );
    }
}
