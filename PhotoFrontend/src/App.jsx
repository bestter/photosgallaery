import Gallery from './pages/Gallery';
import Login from './pages/Login';
import Register from './pages/Register';
import Dashboard from './pages/Dashboard';
import Moderation from './pages/Moderation';
import AdminGroups from './pages/AdminGroups';
import AdminGroupRequests from './pages/AdminGroupRequests';
import { Toaster } from 'react-hot-toast';
import ContactFab from './components/ContactFab';
import Join from './pages/Join';
import Contact from './pages/Contact';
import { isTokenExpired } from './authHelper';
import { useEffect } from 'react';
import { fetchCsrfToken } from './api';

function App() {
  const currentPath = window.location.pathname;

  // Logique Closed Loop : L'Auth est obligatoire
  const isLoggedIn = !isTokenExpired();


  const isPublicRoute = currentPath === '/login' || currentPath === '/register' || currentPath.startsWith('/join') || currentPath === '/contact';

  useEffect(() => {
    fetchCsrfToken();
  }, []);

  useEffect(() => {
    if (!isLoggedIn && !isPublicRoute) {
      window.location.href = '/login';
    }
  }, [isLoggedIn, isPublicRoute]);

  if (!isLoggedIn && !isPublicRoute) return null;



  // Routing basique
  let Component = Gallery;
  if (currentPath === '/login') Component = Login;
  else if (currentPath === '/register') Component = Register;
  else if (currentPath.startsWith('/join')) Component = Join;
  else if (currentPath === '/dashboard') Component = Dashboard;
  else if (currentPath === '/moderation') Component = Moderation;
  else if (currentPath === '/admin-groups') Component = AdminGroups;
  else if (currentPath === '/admin-group-requests') Component = AdminGroupRequests;
  else if (currentPath === '/contact') Component = Contact;
  else if (currentPath.startsWith('/group/')) Component = Gallery;

  return (
    <>
      <Toaster />
      <Component />
      <ContactFab />
    </>
  );
}

export default App;