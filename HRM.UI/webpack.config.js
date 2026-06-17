console.log("🧩 Custom Webpack Loaded!");

const path = require('path');
module.exports = {
  resolve: {
    alias: {
      '@env': path.resolve(__dirname, 'src/environments'),
    },
  },
};