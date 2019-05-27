import React from 'react';
import { BrowserRouter, Route, Switch } from 'react-router-dom';
import Layout from './components/Layout';
import Documentation from './components/Documentation';
import Home from './components/Home';

export default class App extends React.Component
{
    displayName = App.name

    render()
    {
        return (
            <BrowserRouter>
                <Layout>
                    <Switch>
                        <Route path="/" exact component={Home} />
                        <Route path="/docs" component={Documentation} />
                        <Route path="/music" component={Home} />
                        <Route path="/admin" component={Home} />
                        <Route path="/stats" component={Home} />
                    </Switch>
                </Layout>
            </BrowserRouter>
        );
    }
}