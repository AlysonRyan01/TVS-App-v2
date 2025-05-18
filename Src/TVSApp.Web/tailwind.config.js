/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./**/*.{html,js,razor}"
  ],
  theme: {
    colors: {
      "background" : "#131126",
      "white" : "#FFFFFF",
      "titlecolor": "#262626",
      "roxo": "#7C45AD",
      "cinza": "#F3F4F6"
    },
    extend: {
      fontFamily: {
        sans: ["Open Sans", "sans-serif"],
        serif: ["Titillium Web", "sans-serif"],
        playpen: ["Playpen Sans Arabic", "cursive"]
      },
    },
  },
  plugins: [],
}