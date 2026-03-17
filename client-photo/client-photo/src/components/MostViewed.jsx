import React, { useState } from 'react';
import Gallery from './Gallery';

const MostViewed = ({ token, setToken }) => {
    const [count, setCount] = useState(10);

    return (
        <div className="container mx-auto p-6">
            <div className="flex flex-col md:flex-row justify-between items-center mb-8 gap-4">
                <h1 className="text-3xl font-extrabold bg-gradient-to-r from-orange-400 to-rose-500 bg-clip-text text-transparent flex items-center gap-2">
                    🔥 Les Plus Vues
                </h1>
                
                <div className="flex items-center gap-3 bg-primary px-5 py-2.5 rounded-xl shadow-sm border border-accent transition-colors">
                    <label htmlFor="count-select" className="text-text-color font-medium">
                        Afficher le Top :
                    </label>
                    <select
                        id="count-select"
                        value={count}
                        onChange={(e) => setCount(Number(e.target.value))}
                        className="bg-primary border border-accent text-text-color text-sm rounded-lg focus:outline-none focus:ring-2 focus:ring-accent block p-2 transition-colors cursor-pointer"
                    >
                        <option value={10}>10</option>
                        <option value={20}>20</option>
                        <option value={50}>50</option>
                        <option value={100}>100</option>
                    </select>
                </div>
            </div>
            
            <div className="-mx-6">
                <Gallery 
                    token={token} 
                    setToken={setToken}
                    customEndpoint={`/photos/most-viewed?count=${count}`}
                    hideUpload={true}
                    title={false}
                    disableReverse={true}
                />
            </div>
        </div>
    );
};

export default MostViewed;
