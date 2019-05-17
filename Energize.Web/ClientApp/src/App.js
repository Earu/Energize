import React from 'react';
import { BrowserRouter, Route, Switch } from 'react-router-dom';
import Layout from './components/Layout';
import Documentation from './components/Documentation';

export default class App extends React.Component
{
    displayName = App.name

    render()
    {
        return (
            <BrowserRouter>
                <Layout>
                    <Switch>
                        <Route path="/" exact component={Documentation} />
                        <Route path="/docs" component={Documentation} />
                        <Route path={"music"} />
                        <Route path={"admin"} />
                        <Route path={"stats"} />
                    </Switch>
                </Layout>
            </BrowserRouter>
        );
    }
}