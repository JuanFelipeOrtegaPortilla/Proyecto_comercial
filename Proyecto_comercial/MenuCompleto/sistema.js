(function(){
 document.querySelectorAll('aside a').forEach(function(a){if(new URL(a.href).pathname.toLowerCase()===location.pathname.toLowerCase())a.setAttribute('aria-current','page');});
 var tabs=Array.from(document.querySelectorAll('[data-tab]'));if(!tabs.length)return;
 var active='empresa';try{active=sessionStorage.getItem('carchi-tab-empresa')||active;}catch(e){}
 function select(key,focus){if(!tabs.some(function(t){return t.dataset.tab===key;}))key='empresa';tabs.forEach(function(t){var on=t.dataset.tab===key;t.setAttribute('aria-selected',on?'true':'false');t.tabIndex=on?0:-1;if(on&&focus)t.focus();});document.querySelectorAll('[data-tab-panel]').forEach(function(p){p.hidden=p.dataset.tabPanel!==key;});try{sessionStorage.setItem('carchi-tab-empresa',key);}catch(e){}}
 tabs.forEach(function(t,i){t.addEventListener('click',function(){select(t.dataset.tab,false);});t.addEventListener('keydown',function(e){if(e.key==='ArrowRight'||e.key==='ArrowLeft'){e.preventDefault();select(tabs[(i+(e.key==='ArrowRight'?1:tabs.length-1))%tabs.length].dataset.tab,true);}});});select(active,false);
})();