import Gallery from './pages/Gallery';
import Login from './pages/Login';
import Register from './pages/Register';
import Dashboard from './pages/Dashboard';
import Moderation from './pages/Moderation';
import { Toaster } from 'react-hot-toast';

function App() {
  const currentPath = window.location.pathname;

  return (
    <>
      <Toaster />
      {currentPath === '/login' ? <Login /> : currentPath === '/register' ? <Register /> : currentPath === '/dashboard' ? <Dashboard /> : currentPath === '/moderation' ? <Moderation /> : <Gallery />}
    </>
  );
}

export default App;