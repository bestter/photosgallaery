import Gallery from './pages/Gallery';
import Login from './pages/Login';
import Register from './pages/Register';
import Dashboard from './pages/Dashboard';
import Moderation from './pages/Moderation';
import AdminGroups from './pages/AdminGroups';
import { Toaster } from 'react-hot-toast';
import Join from './pages/Join';
import { isTokenExpired } from './authHelper';

function App() {
  const currentPath = window.location.pathname;

  // Logique Closed Loop : L'Auth est obligatoire
  const token = localStorage.getItem('token');
  const isLoggedIn = token && !isTokenExpired(token);

  const isPublicRoute = currentPath === '/login' || currentPath === '/register' || currentPath.startsWith('/join');

  if (!isLoggedIn && !isPublicRoute) {
      window.location.href = '/login';
      return null;
  }

  // Routing basique
  let Component = Gallery;
  if (currentPath === '/login') Component = Login;
  else if (currentPath === '/register') Component = Register;
  else if (currentPath.startsWith('/join')) Component = Join;
  else if (currentPath === '/dashboard') Component = Dashboard;
  else if (currentPath === '/moderation') Component = Moderation;
  else if (currentPath === '/admin-groups') Component = AdminGroups;
  else if (currentPath.startsWith('/group/')) Component = Gallery;

  return (
    <>
      <Toaster />
      <Component />
    </>
  );
}

export default App;