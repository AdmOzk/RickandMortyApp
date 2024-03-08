import React from 'react';
import { createRoot } from 'react-dom/client';
import PaginationFrontend from './PaginationFrontend'; // Buradaki yolu dosyanın yerine göre ayarlayın

const root = createRoot(document.getElementById('root'));
root.render(<PaginationFrontend />);
