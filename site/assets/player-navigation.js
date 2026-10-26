let navigationLanguage = navigator.language.toLowerCase();
try {
  const saved = localStorage.getItem('ballance.site.language');
  if (saved === 'ru' || saved === 'en') navigationLanguage = saved;
} catch {}
const navigationLocale = navigationLanguage.startsWith('ru') ? 'ru'
  : navigationLanguage.startsWith('zh') ? (/hant|tw|hk/.test(navigationLanguage) ? 'zh-Hant' : 'zh-Hans') : 'en';
const navigationLabels = {
  en: ['About the games', 'Play ', 'Full screen'],
  ru: ['Об играх', 'Играть в ', 'Полный экран'],
  'zh-Hans': ['关于游戏', '游玩 ', '全屏'],
  'zh-Hant': ['關於遊戲', '遊玩 ', '全螢幕']
}[navigationLocale];
const home = document.querySelector('[data-edition-home], #home');
if (home) home.textContent = navigationLabels[0];
const other = document.querySelector('[data-edition-switch]');
if (other) {
  const product = other.getAttribute('href').includes('original.html') ? 'Ballance' : 'Reballance';
  other.textContent = navigationLabels[1] + product;
}
const fullScreenButton = document.querySelector('#fullscreen');
if (fullScreenButton) fullScreenButton.textContent = navigationLabels[2];
if (document.querySelector('#edition-navigation')) {
  // Keep the legacy Unity player's keyboard capture from swallowing host navigation.
  window.addEventListener('keydown', event => {
    if (event.key === 'Tab') event.stopImmediatePropagation();
  }, true);
}
