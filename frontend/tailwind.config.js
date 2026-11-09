// Colours come from design/tokens.json, the only place they are defined. Charts read the same file.
const tokens = require('./projects/bank-app/src/design/tokens.json');

/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./projects/bank-app/src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      colors: tokens.color,
    },
  },
  plugins: [],
};
