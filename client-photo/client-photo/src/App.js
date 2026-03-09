import React, { useState } from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';

// Import de tes composants
import Navbar from './components/Navbar';
import Gallery from './components/Gallery';
import Login from './components/Login';
import Register from './components/Register';
import AdminDashboard from './components/AdminDashboard';
import AdminRoute from './components/AdminRoute';
import Footer from './components/Footer';

function App() {
    // L'état centralisé pour savoir si l'utilisateur est connecté
    const [token, setToken] = useState(localStorage.getItem('token'));

    return (
        <Router>
            <div className="min-h-screen bg-gray-50 font-sans">
                
                <Toaster position="bottom-right" reverseOrder={false} />
                {/* 1. LA BARRE DE NAVIGATION EN HAUT DE TOUT */}
                <Navbar token={token} setToken={setToken} />

                {/* 2. LE CONTENU PRINCIPAL QUI CHANGE SELON L'URL */}
                <main className="pb-12">
                    <Routes>
                        {/* Pages publiques */}
                        {/* La galerie normale, sans filtre */}
      <Route path="/" element={<Gallery />} />
      
      {/* La même galerie, mais avec un paramètre dynamique dans l'URL */}
      <Route path="/tags/:tagName" element={<Gallery />} />
      
                        <Route path="/login" element={<Login setToken={setToken} />} />
                        <Route path="/register" element={<Register />} />

                        {/* Page protégée de l'Admin */}
                        <Route 
                            path="/admin" 
                            element={
                                <AdminRoute token={token}>
                                    <AdminDashboard />
                                </AdminRoute>
                            } 
                        />
                    </Routes>
                </main>
                <Footer />
            </div>
        </Router>
    );
}

export default App;