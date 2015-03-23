#!/usr/bin/env python
# -*- coding: utf-8 -*-

import os, sys, inspect
from datetime import datetime
import json

#from threading import Thread
import Queue
from PyQt4.uic.Compiler.qtproxies import QtGui

src_dir = os.path.dirname(inspect.getfile(inspect.currentframe()))
arch_dir = 'LeapSDK/lib/x64' if sys.maxsize > 2**32 else 'LeapSDK/lib/x86' #Leap.py se musi nakopirovat do obou
sys.path.insert(0, os.path.abspath(os.path.join(src_dir, arch_dir)))

import Leap
from Leap import CircleGesture, KeyTapGesture, ScreenTapGesture, SwipeGesture
import cv2
import numpy as np

from PyQt4 import QtGui
from PyQt4 import QtCore



class MyLeap():
    def __init__(self, headOptimization):
                
        self.controller = Leap.Controller()
        #self.controller.enable_gesture(Leap.Gesture.TYPE_CIRCLE)
                     
                
        if headOptimization:
            self.controller.set_policy_flags(Leap.Controller.POLICY_OPTIMIZE_HMD|Leap.Controller.POLICY_BACKGROUND_FRAMES) #optimalizace pro pohled zvrchu
        else:
            self.controller.set_policy_flags(Leap.Controller.POLICY_BACKGROUND_FRAMES) #bezi i bez focusu OS
                            
        
                    
        #self.controller.config.set("Gesture.Circle.MinRadius", 3.0)
        #self.controller.config.set("Gesture.Circle.MinArc", .3)
        self.controller.config.save()
        

                
        
    def get_finger_position(self, index = Leap.Finger.TYPE_INDEX):
        frame = self.controller.frame()
        hands = frame.hands
        
        found = False
        for hand in hands:
            if hand.is_right:
                for pointable in hand.pointables:
                    if pointable.is_finger:
                        finger = Leap.Finger(pointable)
                        if finger.type() == index:
                            position =  pointable.tip_position
                            found = True
        
        if found:
            return [position[0],position[1],position[2]], frame.id

        else:
            return [0,0,0],0

            
    def avg_finger_position(self, count=10, index = Leap.Finger.TYPE_INDEX):
        avgpos = [0,0,0]
        lastframe = 0
        for i in range(0, count):
            while True:
                position, frame = self.get_finger_position(index)
                if position and lastframe != frame:
                    avgpos[0] += position[0]
                    avgpos[1] += position[1]
                    avgpos[2] += position[2]
                    lastframe = frame
                    break
        #print avgpos
        return [avgpos[0]/count,avgpos[1]/count,avgpos[2]/count]        
    
  
    def isHand(self, index = Leap.Finger.TYPE_INDEX):
        '''pro zjisteni jestli je v obraze naka ruka, aby se zbytecne nevykreslovalo, pri pozdejsich optimalizacich zmenit nebo vyhodit'''
        frame = self.controller.frame()
        hands = frame.hands        
        found = False
        for hand in hands:
            if hand.is_right:
                for pointable in hand.pointables:
                    if pointable.is_finger:
                        finger = Leap.Finger(pointable)
                        if finger.type() == index:
                            position =  pointable.tip_position
                            found = True
        return found
    
    def get_pinch_strength(self, index = Leap.Finger.TYPE_INDEX):
        frame = self.controller.frame()
        hands = frame.hands        
        for hand in hands:
            if hand.is_right:
                return hand.pinch_strength
        return 0
                            
                                    
        
        
class Calibration(QtCore.QThread):
    draw_coordinates = QtCore.pyqtSignal(object)
    clean = QtCore.pyqtSignal(object)
    draw_update = QtCore.pyqtSignal(object)
    confirm_points = QtCore.pyqtSignal(object)
    result_points = QtCore.pyqtSignal(object)
    
    def __init__(self, values, queue):
        super(Calibration, self).__init__()
        
        self.shouldRun = True
        self.queue = queue
                      
        
        self.mode = values['mode']
        self.width = values['width']
        self.height = values['height']
        self.headOptimization = values['headOptimization']
        self.shift = values['shift']
        
        self.leap = MyLeap(self.headOptimization)
        
        self.mtx = np.float32([[  962,   0.,   self.width/2], #962 doma 2564 ve skole
                               [  0.,   962,   self.height/2],
                               [  0.,   0.,   1.]])     
        
        self.dist = np.float32([[ 0., 0.,  0., 0.,  0.]])        
                
        np.set_printoptions(precision=8,suppress=True)                     
        
        if self.mode == "new":
            self.imagePoints = []#np.array([], np.float32)
            self.worldPoints = []#np.array([], np.float32)                         
            self.count = values['count']            
            self.file = values['file']
                    
        elif self.mode == "load" or self.mode == "reproj":
            self.worldPoints = values['worldPoints']
            self.imagePoints = values['imagePoints']
            #self.mtx = np.float32(values['mtx'])
            #self.dist = np.float32(values['dist'])
            #self.rvecs = [np.float32(values['rvecs'])]
            #self.tvecs = [np.float32(values['tvecs'])]
                         
                        
        
                
    def sequence(self, x, y):
        '''
        vykresleni kalibracniho bodu a nacteni souradnic prstu v 3D prostoru Leapu
        '''                                                
        #self.window.draw(x,y)
        self.draw_coordinates.emit([x,y])
        #self.window.update()
        self.draw_update.emit(None)
        waitTime = 1
        
        #ziskani bodu po ustalenem cekani prstu na jednom miste po dobu jedne vteriny
        start = datetime.now()
        points=[]
        repeat = False
        while True:
            if repeat:
                start = datetime.now()
                points = []
                repeat = False
            if len(points) == 0:            
                points.append(self.leap.avg_finger_position(10))
            point = self.leap.avg_finger_position(10)
            
            #pokud se prst pohl v jakekoliv ose o znacnou cast, je treba snimat znova
            shift = 10
            for p in points:
                if abs(p[0]-point[0]) > shift:
                    repeat = True
                    break
                elif abs(p[1]-point[1]) > shift:
                    repeat = True                  
                    break
                elif abs(p[2]-point[2]) > shift:
                    repeat = True                   
                    break 
            points.append(point)   
            end = datetime.now()
            interval = end-start
            if interval.seconds >=waitTime:
                if len(points) > 4: #aspon 4 namerene body, tzn dostatecne presne
                    break
                else:
                    repeat = True      
            
        #print x, y
        worldpoint = self.leap.avg_finger_position(10) #TODO mozna radsi prumerovat uz pres ty namereny              
        
        self.clean.emit("")  
        self.draw_update.emit(None)        
        
        return [x,y], worldpoint
        #self.save_points([x,y], worldpoint)        
    
    def calibration(self):                            
        points = []
        i = 0        
        while i < self.count:         
            if not self.shouldRun:
                return   
            points.append(self.sequence(0+self.shift, 0+self.shift))
            if not self.shouldRun:
                return              
            points.append(self.sequence(self.width/2, 0+self.shift))
            if not self.shouldRun:
                return              
            points.append(self.sequence(self.width-self.shift, 0+self.shift))
                  
            if not self.shouldRun:
                return              
            points.append(self.sequence(0+self.shift, self.height/2))
            if not self.shouldRun:
                return              
            points.append(self.sequence(self.width/2, self.height/2))
            if not self.shouldRun:
                return              
            points.append(self.sequence(self.width-self.shift, self.height/2))       
            
            if not self.shouldRun:
                return              
            points.append(self.sequence(0+self.shift, self.height-self.shift))
            if not self.shouldRun:
                return              
            points.append(self.sequence(self.width/2, self.height-self.shift))
            if not self.shouldRun:
                return              
            points.append(self.sequence(self.width-self.shift, self.height-self.shift))
            if not self.shouldRun:
                return              
        
            #===================================================================
            # for point in points:
            #     print >> sys.stderr, str(point)+"  "
            # print >> sys.stderr, "\n---\n"                    
            #  
            # print >>sys.stderr,"namereno OK?"
            # repeat = raw_input()
            # 
            # if repeat != "n":      
            #===================================================================
            
            pointsStr = "image Points, woldPoints\n"
            for point in points:
                pointsStr += str(point[0])+"    "+str(point[1])+"\n"
            self.confirm_points.emit(pointsStr)
            
            while self.queue.empty():
                pass
            ok = self.queue.get(0)        
            
            if ok:
                #imagePoints = [] #kdyz chci body rozdelit na sady a neposilat je zaraz
                #worldPoints = []
                for point in points:
                    self.imagePoints.append(point[0])
                    self.worldPoints.append(point[1])                                                                                                                                             
                i+=1
                #self.imagePoints.append(imagePoints)
                #self.worldPoints.append(worldPoints)                       
            
            points = []
                
    def reprojection(self):        
        points = []
        self.shift += 10 #aby se to lisilo od kalibracnich
        
        for i in range(5):
            for j in range(5):
                if not self.shouldRun:
                    return
                x = self.width/4*j
                y = self.height/4*i
                
                if x < self.width/2 :
                    x+= self.shift
                elif x > self.width/2:
                    x-= self.shift
                    
                if y < self.height/2 :
                    y+= self.shift
                elif y > self.height/2:
                    y-= self.shift                    
                
                points.append(self.sequence(x, y))
                
        self.imagePoints = []
        self.worldPoints = []
        for point in points:
            self.imagePoints.append(point[0])
            self.worldPoints.append(point[1])           
                     
                            
    def reproject(self):
        total = 0
        count = len(self.worldPoints)                 
        for i in range(count):
            if not self.shouldRun:
                return  
            worldPoints = np.float32([[self.worldPoints[i][0],self.worldPoints[i][1],self.worldPoints[i][2]]]).reshape(-1,3) #3D
            imgpts, jac = cv2.projectPoints(worldPoints, self.rvecs[-1], self.tvecs[-1] , self.mtx, self.dist)
            imgpts = imgpts[0][0] #ziskany
            imgdef = self.imagePoints[i] #vychozi
            #print imgdef,imgpts            
            dist = np.sum(np.abs(imgpts-imgdef)**2) #vzdalenost mezi body
            #print dist
            total += dist
        avg = np.sqrt(total/count)
        print "reprojection error:"
        print avg
                
    def calibrate(self): #kalibrace v prostoru            
        if not self.shouldRun:
            return  
                                       
        self.worldPointsNP = np.array([self.worldPoints], np.float32) #upravene pro numpy(NP)
        self.imagePointsNP = np.array([self.imagePoints], np.float32)
                                                                     
        print "worldPoints:"         
        print self.worldPointsNP
        print "---"
        print "imagePoints:"
        print self.imagePointsNP                 
                                                                         
        self.ret, self.mtx, self.dist, self.rvecs, self.tvecs = cv2.calibrateCamera(self.worldPointsNP, self.imagePointsNP, (self.width,self.height) ,self.mtx, self.dist, flags=cv2.CALIB_USE_INTRINSIC_GUESS)            
        
        print "---"
        print "mtx:"
        print self.mtx
        print "---"
        print "rvecs:"
        print self.rvecs
        print "---"
        print "tvecs:"
        print self.tvecs       
        print "---"
        print "dist:"
        print self.dist
        print "---"
        print "ret/reproj error:"
        print self.ret         
        
        if self.mode == "new":
            output = {}
            output['imagePoints'] = self.imagePoints
            output['worldPoints'] = self.worldPoints
            output['width'] = self.width
            output['height'] = self.height
            output['mtx'] = self.mtx.tolist()
            output['dist'] = self.dist.tolist()
            output['rvecs'] = self.rvecs[0].tolist()      
            output['tvecs'] = self.tvecs[0].tolist()
                                                                    
            
            self.file.write(json.dumps(output))        
            self.file.close()                                                 

    def intersection_point_line(self,point,lineStart,lineEnd):
        #https://www.youtube.com/watch?v=uxYaIWhlBTc
        x1 = np.array(lineStart) #vychozi bod a prametricke vyjadreni
        x2 = np.array(lineEnd)
        u = np.subtract(x2,x1) #vektor a parametricke vyjadreni - t

        A = np.array(point)
        d = -np.sum(np.multiply(A,u)) #x+2y-z+d=0 pomocna rovina
        s1 =  np.sum(np.multiply(u,x1))+d #dosazeni do rovnice pro x1
        s2 =  np.sum(np.multiply(u,u)) #dosazeni do rovnice za t
        t = -(s1/s2) 
        
        P =  np.add(x1,np.multiply(u,t)) #prusecik
        
        #dist = np.linalg.norm(P-A)
        return P                       
        
    def test(self):    
        if self.leap.isHand(): #pokud ma cenu vysilat
            if 'self.lastposition' not in locals():
                self.lastposition = 0        
            position, frame = self.leap.get_finger_position()
            
            
            if position != self.lastposition:
                self.lastposition = position
                
                worldPoints = np.float32([[position[0],position[1],position[2]]]).reshape(-1,3) #3D
                # worldPoints = np.float32([position[0],position[1],0]).reshape(-1,3)
                imgpts, jac = cv2.projectPoints(worldPoints, self.rvecs[-1], self.tvecs[-1] , self.mtx, self.dist)
                                          
                
                #print self.rvecs[-1],"\n"
                r = cv2.Rodrigues(self.rvecs[-1])[0]
                #print r,"\n" #3*3 output vector
                rt = np.float32([[  r[0][0],   r[0][1],   r[0][2],   self.tvecs[-1][0]],
                                 [  r[1][0],   r[1][1],   r[1][2],   self.tvecs[-1][1]],
                                 [  r[2][0],   r[2][1],   r[2][2],   self.tvecs[-1][2]]]) #,[ 0,0,0,1] ]
                
                #===============================================================
                # invrt = np.linalg.inv(rt)
                # print invrt
                # 
                # zero = np.float32([0,0,0,1])
                # print np.dot(invrt,zero)
                #===============================================================
                
                #print worldPoints,"\n"
                mtxRT =  np.dot(self.mtx, rt)
                #worldPoints v matici jineho tvaru, pro nasobeni            
                modWP = np.float32([[position[0]],
                                   [position[1]],
                                   [position[2]],
                                   [1]])
                #print modWP
                
                
                #bod promitnuty pro vychozi souradnice projektoru
                proj = np.dot(rt,modWP)
                #print proj,"\n"
                #vysledne prevedene body z prostoru do roviny (lisi se od projectPoints)
                uv = np.dot(mtxRT,modWP)
                uv =  uv/uv[2]
                
                #print uv[0],uv[1],"\n"    
                #print imgpts
                
                #self.window.clean_window()
                self.clean.emit(None)                    
                #self.window.draw(imgpts[0][0][0], imgpts[0][0][1])
                self.draw_coordinates.emit([imgpts[0][0][0],imgpts[0][0][1]]) #project points                  
                #self.draw_coordinates.emit([uv[0][0],uv[1][0]])  #rucni vypocet
                #self.draw_coordinates.emit([imgpts[0][0][0], imgpts[0][0][1], uv[0][0], uv[1][0], self.leap.get_pinch_strength()]) #oboje
                
                
                self.result_points.emit([[round(imgpts[0][0][0],3),round(imgpts[0][0][1],3)],[round(uv[0][0],3),round(uv[1][0],3)]])
                self.draw_update.emit(None)
    
    def run(self):
        if self.mode == "new":
            self.calibration()
        
        if self.shouldRun:                           
            self.calibrate()
        
        if self.shouldRun and self.mode == "reproj":
            self.reprojection()
            self.reproject()
                                                        
        while self.shouldRun:
            self.test()
        
            

class ConfirmDialog(QtGui.QDialog):
    def __init__(self, points, parent = None):
        super(ConfirmDialog, self).__init__(parent,QtCore.Qt.WindowSystemMenuHint | QtCore.Qt.WindowTitleHint)
        
        #modalni okno
        self.setWindowModality(QtCore.Qt.ApplicationModal)
        
        self.setWindowTitle('points OK?')    
        
        self.layout = QtGui.QVBoxLayout(self)
        
        self.pointsLabel = QtGui.QLabel(points)
        self.layout.addWidget(self.pointsLabel)
        
        # OK and Cancel buttons
        self.buttons = QtGui.QDialogButtonBox(
            QtGui.QDialogButtonBox.Ok | QtGui.QDialogButtonBox.Cancel,
            QtCore.Qt.Horizontal, self)
        self.layout.addWidget(self.buttons)        
        
        self.buttons.accepted.connect(self.accept)
        self.buttons.rejected.connect(self.reject)
    
    @staticmethod            
    def getResult(points,parent = None):
        dialog = ConfirmDialog(points,parent)
        result = dialog.exec_()
        return  result == QtGui.QDialog.Accepted   
            
    
class CalibWindow (QtGui.QGraphicsView): #QtGui.QWidget
    
    def __init__(self, values):
        super(CalibWindow, self).__init__()
                
        #context menu
        self.setContextMenuPolicy(QtCore.Qt.DefaultContextMenu)
        
        self.width = values['width']
        self.height = values['height']
        self.queue = Queue.Queue()
        
        self.cleanable = []
        
        #self.view = View(self)
        self.setScene(QtGui.QGraphicsScene())
                
        #obdelnik kam se kresli
        self.setSceneRect(QtCore.QRectF(0, 0, self.width, self.height))
        self.setGeometry(0, 0, self.width+2, self.height+2) #proc? :( jinak scrollbar
        
        desktop = QtGui.QDesktopWidget()        
        
        if desktop.screenCount() == 2: #projektor
            screen = desktop.screenGeometry(1)
            self.move(screen.left(), screen.top())
        
    
        
        #modalni okno
        self.setWindowModality(QtCore.Qt.ApplicationModal)
        
        #oddela title bar
        self.setWindowFlags(QtCore.Qt.FramelessWindowHint)                
        
        self.calibration = Calibration(values, self.queue)
        self.calibration.draw_coordinates.connect(self.on_draw)
        self.calibration.clean.connect(self.on_clean)
        self.calibration.draw_update.connect(self.on_update)
        self.calibration.confirm_points.connect(self.on_confirm)
        self.calibration.result_points.connect(self.on_position)
        self.calibration.daemon = True
        self.calibration.start()        
    
    
    def on_position(self, coordinates):
        projPoints = QtGui.QGraphicsSimpleTextItem(str(coordinates[0][0])+","+str(coordinates[0][1]))
        #projPoints = QtGui.QGraphicsSimpleTextItem(str(coordinates[0][0])+","+str(coordinates[0][1])+"\n"+str(coordinates[1][0])+","+str(coordinates[1][1]))#jedno pres openCV, jedno rucne spocitane        
        self.scene().addItem(projPoints)
        self.cleanable.append(projPoints)      
          
    
    def contextMenuEvent(self, event):
        menu = QtGui.QMenu(self)
        quitAction = menu.addAction("Close")
        action = menu.exec_(self.mapToGlobal(event.pos()))
        if action == quitAction:
            self.close()        
    
    def on_confirm(self, points):
        ok = ConfirmDialog.getResult(points)    
        self.queue.put(ok)
    
    def on_update(self, data):
        self.update()
    
    def on_draw(self, xy):
        #projectPoints
        x = xy[0]
        y = xy[1]        
        r = 33
        pen = QtGui.QPen(QtGui.QColor(QtCore.Qt.blue))
        brush = QtGui.QBrush(pen.color().darker(150))
                
        self.cleanable.append(self.scene().addEllipse(x-r/2,y-r/2,r,r, pen, brush))
        
        if len(xy) > 2:
            #rucni vypocet
            x2 = xy[2]
            y2 = xy[3]    
            pen = QtGui.QPen(QtGui.QColor(QtCore.Qt.green))
            brush = QtGui.QBrush(pen.color().darker(150))
                    
            self.cleanable.append(self.scene().addEllipse(x2-r/2,y2-r/2,r,r, pen, brush))          
            
        if len(xy) > 4: #TODO pri dotyku obrazovky
            strength = xy[4]
            if strength > 1:#0.85:
                pen = QtGui.QPen(QtGui.QColor(QtCore.Qt.black))
                brush = QtGui.QBrush(pen.color().darker(150))
                self.scene().addEllipse(x-strength*10/2, y-strength*10/2, strength*10, strength*10, pen, brush)
                     
            
                      
    def draw(self, x, y, r=33):
        pen = QtGui.QPen(QtGui.QColor(QtCore.Qt.blue))
        brush = QtGui.QBrush(pen.color().darker(150))

        self.scene().addEllipse(x-r/2,y-r/2,r,r, pen, brush)
    
    def on_clean(self, data):
        #for item in self.items():
        for item in self.cleanable:
            self.scene().removeItem(item)
        self.cleanable = []        
        
    def clean_window(self):
        for item in self.items():            
                self.scene().removeItem(item)
         
    def mousePressEvent(self, event):
        if event.button() == QtCore.Qt.LeftButton:
            self.offset = event.pos()
        
        if event.button() == QtCore.Qt.RightButton:
            self.contextMenuEvent(event)
    
    def mouseMoveEvent(self, event):
        x=event.globalX()
        y=event.globalY()
        x_w = self.offset.x()
        y_w = self.offset.y()
        self.move(x-x_w, y-y_w)            
        
    
    def closeEvent(self, *args, **kwargs):
        #pro ukonceni threadu pri zavreni okna        
        self.calibration.shouldRun = False
             
        
class MainWindow(QtGui.QWidget):
    
    def __init__(self):
        super(MainWindow, self).__init__()                                
        
        self.calibWindow = [] #musim mit ukazatel na okno nez dobehne jeho thread, takze jich to muze byt vic zaraz (dve)
        
        self.widthLabel = QtGui.QLabel('Width')
        self.heightLabel = QtGui.QLabel('Height')
        self.countLabel = QtGui.QLabel('Cycles')
        self.shiftLabel = QtGui.QLabel('Shift')

        self.widthEdit = QtGui.QLineEdit()
        self.widthEdit.setText("800")
        self.heightEdit = QtGui.QLineEdit()
        self.heightEdit.setText("600")
        self.countEdit = QtGui.QLineEdit()
        self.countEdit.setText("1")
        self.shiftEdit = QtGui.QLineEdit()
        self.shiftEdit.setText("30")        
        
        
        self.headOptimization = QtGui.QCheckBox("Head mount optimization");
        self.headOptimization.setChecked(True)
        
        self.calibrateButton = QtGui.QPushButton("Calibrate")
        self.calibrateButton.clicked.connect(self.calibrate)
        self.calibrateButton.setMaximumWidth(70)
        
        self.loadButton = QtGui.QPushButton("Load")
        self.loadButton.clicked.connect(lambda: self.loadCalibration("load"))
        self.loadButton.setMaximumWidth(70)
        
        self.reprojButton = QtGui.QPushButton("Reprojection")
        self.reprojButton.clicked.connect(lambda: self.loadCalibration("reproj"))      
        self.reprojButton.setMaximumWidth(70)                

        self.grid = QtGui.QGridLayout()
        self.grid.setSpacing(10)        
        
        #udrzeni centrovani bez roztahovani
        self.grid.setColumnStretch(0,1)
        self.grid.setColumnStretch(3,1)
        self.grid.setRowStretch(0,1)
        self.grid.setRowStretch(7,1)
        
        self.grid.addWidget(self.widthLabel, 1, 1)
        self.grid.addWidget(self.widthEdit, 1, 2)

        self.grid.addWidget(self.heightLabel, 2, 1)
        self.grid.addWidget(self.heightEdit, 2, 2)
        
        self.grid.addWidget(self.countLabel, 3, 1)
        self.grid.addWidget(self.countEdit, 3, 2)
        
        self.grid.addWidget(self.shiftLabel, 4, 1)
        self.grid.addWidget(self.shiftEdit, 4, 2)         

        self.grid.addWidget(self.headOptimization, 5, 2)
        
        self.grid.addWidget(self.calibrateButton, 6, 1)      
        self.grid.addWidget(self.loadButton, 6, 2)
        self.grid.addWidget(self.reprojButton, 7, 2)  

        
        self.setLayout(self.grid) 
                
        self.setGeometry(500, 300, 300, 200)
        self.setWindowTitle('Calibration')    
        self.show()
        
        QtGui.QApplication.focusWidget().clearFocus() #odstrani pocatecni focus
            
        
    def calibrate(self):        
        values = self.getValues()
        values['mode'] = "new"
         
        fname = QtGui.QFileDialog.getSaveFileName(self)
        if fname == "":
            return
                         
        f = open(fname, 'w')          
        values['file'] = f
        
        self.calibWindow.append(CalibWindow(values)) 
        self.calibWindow[-1].show()

            
        
    def loadCalibration(self, mode):
        values = self.getValues()
        values['mode'] = mode        
        fname = QtGui.QFileDialog.getOpenFileName(self)   
        if fname == "":
            return
             
        f = open(fname, 'r')                
        data = f.readline()
        f.close()
        
        data = json.loads(data)
        values['worldPoints'] = data['worldPoints']
        values['imagePoints'] = data['imagePoints']
        values['width'] = data['width']
        values['height'] = data['height']       
        values['mtx'] = data['mtx']
        values['rvecs'] = data['rvecs']
        values['tvecs'] = data['tvecs']
        values['dist'] = data['dist'] 
        values['headOptimization'] = self.headOptimization.isChecked()
        
        self.calibWindow.append(CalibWindow(values)) 
        self.calibWindow[-1].show()
    
    def getValues(self):
        values = {}
        values['width'] = int(self.widthEdit.text())
        values['height'] = int(self.heightEdit.text())
        values['count'] = int(self.countEdit.text())
        values['shift'] = int(self.shiftEdit.text())                
        values['headOptimization'] = self.headOptimization.isChecked()
        return values
    
                 
        
def main():
    #TODO zrusit thread kdyz se prerusi v prubehu kalibrace ne az v testu
    app = QtGui.QApplication(sys.argv)
    ex = MainWindow()
    sys.exit(app.exec_())
        

    
if __name__ == '__main__':
    main()
        