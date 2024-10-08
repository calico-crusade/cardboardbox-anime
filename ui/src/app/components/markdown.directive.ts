import { Directive, ElementRef, Input } from "@angular/core";
import { marked } from 'marked';

import * as hljs from 'highlight.js';

marked.setOptions({
    highlight: (code, lang) => {
        return hljs.default.highlight(lang, code).value;
    }
})

@Directive({
    selector: '[markdown]'
})
export class MarkdownDirective {

    private _current?: string;
    private _marked?: string;

    @Input()
    set markdown(content: string | undefined) {
        if (this._current === content) return;
        
        this._current = content;

        try {
            this._marked = marked.parse(content || '');
        } catch { }
        this.host.nativeElement.innerHTML = this._marked || this._current || '';
    }

    constructor(
        private host: ElementRef
    ) { }
}