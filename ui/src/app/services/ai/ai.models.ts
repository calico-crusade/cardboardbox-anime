import { DbObject, PagedResults } from "../anime/anime.model";

export interface AiRequest {
    prompt: string;
    negativePrompt: string;
    steps: number;
    batchCount: number;
    batchSize: number;
    cfgScale: number;
    seed: number;
    height: number;
    width: number;
}

export interface AiRequestImg2Img extends AiRequest {
    image: string;
    denoiseStrength?: number;
}

export interface AiDbRequest extends AiRequestImg2Img, DbObject {
    profileId: number;

    imageUrl?: string;

    outputPaths: string[];
    generationStart: Date;
    generationEnd?: Date;
    secondsElapsed?: number;
}

export type AiResults = PagedResults<AiDbRequest>;

export const DEFAULT_REQUEST : AiRequestImg2Img = {
    prompt: 'a girl,Phoenix girl,fluffy hair,war,a hell on earth,Beautiful and detailed explosion,Cold machine,Fire in eyes,World War,burning,Metal texture,Exquisite cloth,Metal carving,volume,best quality,normal hands,Metal details,Metal scratch,Metal defects,{{masterpiece}},best quality,official art,4k,best quality,extremely detailed CG unity 8k,illustration,highres,masterpiece,contour deepening,Azur Lane,Girls Front,Magical,Cloud Map Plan,contour deepening,long-focus,Depth of field,a cloudy sky,Black smoke,smoke of gunpowder,long-focus,Mature,resolute eyes,burning,burning sky,burning hair,Burn oneself in flames,fighting,Covered in blood,complex pattern,battleing,Flying flames,Flame whirlpool,Doomsday Scenes,float,Splashing blood,on the battlefield,Bloody scenes,Good looking flame,Exquisite Flame,Exquisite Blood,photorealistic,Watercolor,colourful,(((masterpiece))),best quality,illustration,beautiful detailed glow,detailed ice,beautiful detailed water,red moon,(magic circle:1,2),(beautiful detailed eyes),expressionless,beautiful detailed white gloves,own hands clasped,(floating palaces:1.1),azure hair,disheveled hair,long bangs,hairs between eyes,dark dress,(dark magician girl:1.1),black kneehighs,black ribbon,white bowties,midriff,{{{half closed eyes}}},,big forhead,blank stare,flower,large top sleeves,,(((masterpiece))),best quality,illustration,(beautiful detailed girl),beautiful detailed glow,detailed ice,beautiful detailed water,(beautiful detailed eyes),expressionless,beautiful detailed white gloves,(floating palaces:1.2),azure hair,disheveled hair,long bangs,hairs between eyes,(skyblue dress),black ribbon,white bowties,midriff,{{{half closed eyes}}},,big forhead,blank stare,flower,large top sleeves,(((ice crystal texture wings)),(((ice and fire melt)))',
    negativePrompt: '(((ugly))),(((duplicate))),((morbid)),((mutilated)),(((tranny))),mutated hands,(((poorly drawn hands))),blurry,((bad anatomy)),(((bad proportions))),extra limbs,cloned face,(((disfigured))),(((more than 2 nipples))),((((missing arms)))),(((extra legs))),mutated hands,(((((fused fingers))))),(((((too many fingers))))),(((unclear eyes))),lowers,bad anatomy,bad hands,text,error,missing fingers,extra digit,fewer digits,cropped,worst quality,low quality,normal quality,jpeg artifacts,signature,watermark,username,blurry,bad feet,text font ui,malformed hands,long neck,missing limb,(mutated hand and finger: 1.5),(long body: 1.3),(mutation poorly drawn: 1.2),disfigured,malformed mutated,multiple breasts,futa,yaoi',
    steps: 39,
    batchCount: 1,
    batchSize: 1,
    cfgScale: 4.5,
    seed: -1,
    height: 1024,
    width: 512,
    image: '',
    denoiseStrength: 0.2
}