import pyglet
from pyglet.window import key
window = pyglet.window.Window()
label = pyglet.text.Label('Foobar',font_name='Times New Roman',font_size=36,x=window.width//2,y=window.height//2,anchor_x='center',anchor_y='center')


class MusicSystem:
    """Manager for the game's music.
        Planned functionality:
            Should be able to loop a track,
            Play an interruption music cue,
            return to where the loop left off before the interruption
            or switch to a new track after interruption ends"""
    def __init__(s):
        #Component.__init__(s,owner)
        s.currentTrack = None
        s.currentStinger = None
        s.music = pyglet.media.Player()
        s.stinger = pyglet.media.Player()

    def update(s):
    	#If we're in a stinger, check and see if it's finished
    	#if it is finished, trun the regular music back on if it exists
    	print("Current stinger:", s.currentStinger)
    	print("Stinger playing:", s.stinger.playing)
        print("Current track:", s.currentTrack)
    	if(s.currentStinger!=None):
    		if(s.stinger.playing == False):
    			s.currentStinger = None
    			if(s.currentTrack != None):
    				s.music.play()

    		#check stinger state. If it's end
    def stinger_interrupt(s,stinger):
    	if(s.currentTrack!=None):
    		s.music.pause()
    		s.currentStinger=stinger
    		s.stinger.queue(stinger)
    		s.stinger.play()
    		s.stinger.eos_action= s.stinger.EOS_PAUSE

    def stinger_transition(s,stinger,nextTrack):
    	s.start_track(nextTrack)
    	s.stinger_interrupt(stinger)

    def start_track(s,newTrack):
    	#if the currently playing music is the same as the music we're trying to play, don't do anything
    	if(s.currentTrack==newTrack):
    		return

    	s.music.queue(newTrack)
    	if(s.currentTrack == None):
    		s.currentTrack=newTrack
    		s.music.play()
    		s.music.eos_action = s.music.EOS_LOOP
    	else:
    		s.currentTrack=newTrack
    		s.music.next()


    def start_track_from_path(s,path):
    	s.start_track(pyglet.resource.media(path,streaming=False))
    def pause_music(s):
    	s.music.pause()
    	s.stinger.pause()


jukebox = MusicSystem()
@window.event
def on_draw():
    window.clear()
    label.draw()
    jukebox.update()


lightstroke = pyglet.resource.media('ijimusic/secintro.mp3',streaming=True)
darkstroke = pyglet.resource.media('ijimusic/dark.mp3',streaming=True)
sevenfour = pyglet.resource.media('ijimusic/sec5.mp3',streaming=True)
hero3d = pyglet.resource.media('ijimusic/hero3d.mp3',streaming=True)
#jukebox.start_track(music)

@window.event
def on_key_press(symbol,modifiers):
    foo = 0
    try:
        if symbol==key.Q:
            jukebox.start_track(sevenfour)
        elif symbol==key.W:
            jukebox.start_track(hero3d)
        elif symbol==key.A:
            jukebox.stinger_interrupt(lightstroke)
        elif symbol==key.S:
            jukebox.stinger_interrupt(darkstroke)
        elif symbol==key.Z:
            jukebox.stinger_transition(lightstroke,sevenfour)
        elif symbol==key.X:
            jukebox.stinger_transition(darkstroke,hero3d)
    except pyglet.media.MediaException:
        print 'whatever'

pyglet.clock.schedule_interval(lambda dt: jukebox.update, 0.1)
pyglet.app.run()
