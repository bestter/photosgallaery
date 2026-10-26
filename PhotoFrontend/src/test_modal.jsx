import React from 'react';
import { createRoot } from 'react-dom/client';
import ImageModal from './components/ImageModal';
import { I18nextProvider } from 'react-i18next';
import i18n from './i18n';
import './index.css';

const photo = {
  id: "1",
  fullUrl: "https://images.unsplash.com/photo-1541963463532-d68292c34b19?w=800",
  title: "A nice photo",
  uploaderUsername: "johndoe",
  likesCount: 42
};

const root = createRoot(document.getElementById('root'));
root.render(
  <I18nextProvider i18n={i18n}>
    <ImageModal photo={photo} onClose={() => {}} />
  </I18nextProvider>
);
