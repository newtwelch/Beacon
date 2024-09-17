/** @type {import('tailwindcss').Config} */
module.exports = {
    content: ['./**/*.{razor,html}'],
    theme: {
        extend: {
            colors: {
                'd-text': '#9A989A',
                'd-text-accent': '#F5F3F5',
                'd-background': '#0F0F0F',
                'd-container': '#19191A',
                'd-sidepanel': '#252527',
                'l-text': '#51515E',
                'l-text-accent': '#18181F',
                'l-background': '#D3D5DB',
                'l-container': '#E4E5E9',
                'l-sidepanel': '#F5F3F5',
                'd-red': '#E06262',
                'd-orange': '#CE9A4C',
                'd-green': '#40B26A',
                'd-primary': '#235DDA',
                'l-red': '#E85E5E',
                'l-orange': '#E7AB50',
                'l-green': '#14E76A',
                'l-primary': '#215FE5'
            },
            fontFamily: {
                ark: ['arkicons', 'sans'],
                gont: ['Gontserrat', 'sans']
            }
        },
    },
    darkMode: 'class',
    future: {
        hoverOnlyWhenSupported: true,
    },
    plugins: [
        require('@tailwindcss/forms')
    ],
}

