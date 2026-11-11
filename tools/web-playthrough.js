// Plays the browser client from the opening to the finished first return, by the page's own actions only
// (scripted, not human). Usage: tools/build-web.sh, serve build/web on a port, then
//   node tools/web-playthrough.js http://localhost:8765 <seed> [screenshot-dir]
const { chromium } = require('playwright');
const [base, seed, shots] = [process.argv[2] || 'http://localhost:8765', process.argv[3] || '42', process.argv[4]];
(async () => {
  const browser = await chromium.launch({ args: ['--use-gl=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
  const page = await browser.newPage({ viewport: { width: 1400, height: 860 } });
  const errors = [];
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text().slice(0, 300)); });
  page.on('pageerror', e => errors.push('PAGE ' + String(e).slice(0, 300)));
  const shot = async name => { if (shots) { await page.waitForTimeout(250); await page.screenshot({ path: shots + '/' + name + '.png' }); } };
  await page.goto(base + '/index.html?q=off&seed=' + seed);
  await page.waitForSelector('#boot', { state: 'detached', timeout: 90000 });
  await page.click('#fullBody button');                                   // read the opening
  // The first life, as the scripted clicker of P2PresentationTests plays it.
  const result = await page.evaluate(async () => {
    const b = window.butterfly, cmd = (key, test) => {
      const g = (b.state.everything || []).find(x => x.key === key);
      const a = g && g.items.find(test || (() => true));
      return a ? a.command : null;
    };
    for (let month = 0; month < 400; month++) {
      for (let guard = 0; guard < 12; guard++) {
        if (b.state.phase === 'Ended') return 'ended';
        const card = b.state.decision;
        let c = card ? (card.choices.find(x => x.command === 'choose fountain') || card.choices[0]).command : null;
        c = c || cmd('machine', a => a.command.startsWith('restore '))
              || cmd('machine', a => a.command === 'assess' || a.command.startsWith('repair ') || a.command.startsWith('exchange '))
              || cmd('commissions', a => a.command.startsWith('commission look ')) || cmd('challenges') || cmd('work');
        if (!c) break;
        await b.act(c);
      }
      if (b.state.phase === 'Ended') return 'ended';
      if (b.state.hud.machineReady) return 'ready after ' + month + ' months';
      await b.act('end');
    }
    return 'machine never ready';
  });
  console.log('first life:', result, '·', await page.textContent('#facts'));
  if (result.startsWith('ready')) {
    await shot('10-ready');
    await page.click('text=Prepare to leave…');
    await page.waitForSelector('#full:not([hidden])');
    await shot('11-departure');
    await page.click('text=Leave now');
    await page.waitForSelector('text=Go on');
    for (let k = 0; k < 8 && await page.$('text=Go on'); k++) await page.click('text=Go on');
    await shot('12-arrival');
    await page.click('text=Walk into the city');
    await shot('13-return-city');
    const sites = await page.evaluate(() => butterfly.state.ret.sites.length);
    for (let k = 0; k < sites; k++) {
      if (await page.evaluate(() => butterfly.state.ret.canFinish)) break;
      const n = await page.evaluate(() => butterfly.state.ret.sites.find(s => s.state === 0).number);
      await page.evaluate(n => butterfly.openPanel('site:' + n), n);
      await page.click('#panelBody .acts button');                      // go and look
      await page.waitForTimeout(150);
      if (k === 0) await shot('14-site');
      const closer = await page.$('#panelBody .acts button');
      if (closer) await closer.click();
      await page.waitForTimeout(150);
      if (k === 0) await shot('15-site-closer');
    }
    await page.click('#panelClose');
    await page.click('text=Finish looking');
    await page.waitForTimeout(300);
    await shot('16-after-return');
    console.log('phase:', await page.evaluate(() => butterfly.state.phase), '· sites seen:', await page.evaluate(() => butterfly.state.ret.seen));
  } else if (result === 'ended') {
    await shot('10-ended');
    console.log('ending:', (await page.textContent('#fullBody')).slice(0, 300));
  }
  console.log('errors:', JSON.stringify(errors));
  await browser.close();
  process.exit(errors.length ? 1 : 0);
})().catch(e => { console.log('FAILED', e.message.slice(0, 600)); process.exit(1); });
