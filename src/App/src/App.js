import React, { useState, useEffect } from 'react';
import { ipcRenderer } from "electron";
import Appbar from './components/Appbar';
import Calls from './components/Calls';
import GraphsPanel from './components/GraphsPanel';
import Caller from './components/Caller';
import ResultLog from './components/ResultLog';

const App = () => {
  const [results, setResults] = useState({
    data: null,
    isLoading: true,
    init: true,
  })
  const [users, setusers] = useState([]);
  
  useEffect(() => {
    ipcRenderer.on('getusers', (event, response) => {
      setusers([...response]);
    })
    return () => {
      ipcRenderer.removeAllListeners('greeting');
      ipcRenderer.removeAllListeners('getusers');
    }
  },[users]);

  const run = calls => {
    setResults({ data:null, isLoading: true , init: false })
    console.log('fetching results')
    ipcRenderer.send('results',{calls});
    ipcRenderer.on('res', (event, response) => {
      console.log(response)
      setResults({init: false, data: response, isLoading: false})
    })
  }

  return (
    <div className="App">
     <Appbar/>
     <Calls run={run}/>
     <Caller users={users}/>
     <GraphsPanel {...results}/>
     <ResultLog {...results}/>
    </div>
  );
}

export default App;
